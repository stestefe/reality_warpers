# LAB3

import socket
import json
import cv2
import numpy as np
import pyrealsense2 as rs
from MediaPipe import MediaPipe
from collections import defaultdict, deque
import threading
import time

from scipy.linalg import lstsq
from scipy.spatial.transform import Rotation

HOST = "127.0.0.1" # localhost
PORT = 13456


# NOTE: Arcuo ID for -> Reload: 2
# NOTE: Arcuo Id for -> Cart: 5, 5, 10, 50, 100, 125 

# TODO: edge case check only one has to be running either mediapipe or arcuo markers

def socket_client():
    mp = MediaPipe()
    
    pipeline = rs.pipeline()
    config = rs.config()

    pipeline_wrapper = rs.pipeline_wrapper(pipeline)
    pipeline_profile = config.resolve(pipeline_wrapper)
    device = pipeline_profile.get_device()
    device_product_line = str(device.get_info(rs.camera_info.product_line))

    found_rgb = False
    for s in device.sensors:
        if s.get_info(rs.camera_info.name) == 'RGB Camera':
            found_rgb = True
            break
    if not found_rgb:
        print("[main] The demo requires Depth camera with Color sensor")
        exit(0)

    config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
    config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)

    arucoDict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_6X6_1000)
    arucoParam = cv2.aruco.DetectorParameters()
    arucoDetector = cv2.aruco.ArucoDetector(arucoDict, arucoParam)

    pipeline.start(config)
    align_to = rs.stream.color
    align = rs.align(align_to)

    T_matrix = None
    calibration_samples = []
    CALIBRATION_SAMPLES_NEEDED = 5
    
    arcuo_coordinates = {}
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.connect((HOST, PORT))
            sock.setblocking(0)
            
            while True:
                frames = pipeline.wait_for_frames()
                aligned_frames = align.process(frames)
                depth_frame = aligned_frames.get_depth_frame()
                color_frame = aligned_frames.get_color_frame()
                
                if not depth_frame or not color_frame:
                    continue

                color_image = np.asanyarray(color_frame.get_data())
                # depth_image = np.asanyarray(depth_frame.get_data())    

                detection_results = mp.detect(color_image)
                color_image = mp.draw_landmarks_on_image(color_image, detection_results)

                corners, ids, _ = arucoDetector.detectMarkers(color_image)
                # print(corners, ids, flush = True)
                intrinsics = depth_frame.profile.as_video_stream_profile().get_intrinsics()
                
                skeleton_data = mp.skeleton(color_image, detection_results, depth_frame)

                # early exit
                if (skeleton_data is None or ids is None):
                    cv2.namedWindow('RealSense', cv2.WINDOW_AUTOSIZE)
                    color_image = cv2.flip(color_image, 1)
                    cv2.imshow('RealSense', color_image)
                    cv2.waitKey(1)
                    continue

                skeleton_points = np.array([
                    # [skeleton_data['Head_x'], skeleton_data['Head_y'], skeleton_data['Head_z']],
                    [skeleton_data['LHand_x'], skeleton_data['LHand_y'], skeleton_data['LHand_z']],
                    [skeleton_data['RHand_x'], skeleton_data['RHand_y'], skeleton_data['RHand_z']],
                    # [skeleton_data['LFoot_x'], skeleton_data['LFoot_y'], skeleton_data['LFoot_z']],
                    # [skeleton_data['RFoot_x'], skeleton_data['RFoot_y'], skeleton_data['RFoot_z']]
                ])

                # print("MediaPipe Skeleton:", skeleton_data, flush=True)
                
                if ids is not None:
                    for id, marker in zip(ids,corners):
                        valid_coordinates = []

                        for corner in marker[0]:
                            x,y = corner
                            depth = depth_frame.get_distance(int(x), int(y))

                            if depth > 0:
                                coord = rs.rs2_deproject_pixel_to_point(intrinsics, [x, y], depth)
                                valid_coordinates.append(coord)
                        
                        if len(valid_coordinates) == 4:
                            avg_coordinates = np.mean(valid_coordinates, axis = 0)
                            # print("HELLOOOOOOOOOOOOOOOOOOOOOOOOOOO -------", id, avg_coordinates)
                            arcuo_coordinates[int(id[0])] = avg_coordinates
                            
                            # append arcuo marker to environment
                            #skeleton_points.append([avg_coordinates[0], avg_coordinates[1], avg_coordinates[2]])
                            skeleton_points = np.append(skeleton_points, [avg_coordinates], axis=0)

                    # color_image = cv2.aruco.drawDetectedMarkers(color_image, corners, ids)
                    # print("IDS", ids)
                    # print("Corners", corners)
                    cv2.aruco.drawDetectedMarkers(color_image, corners, ids)
                else:
                    arcuo_coordinates = {}

                try:
                    msg = receive(sock)
                    anchors = msg['listOfAnchors']
                    
                    unity_points = np.array([
                        # [anchors[0]['position']['x'], anchors[0]['position']['y'], anchors[0]['position']['z']],  # head
                        [anchors[0]['position']['x'], anchors[0]['position']['y'], anchors[0]['position']['z']],  # left hand
                        [anchors[1]['position']['x'], anchors[1]['position']['y'], anchors[1]['position']['z']],   # right hand
                        # 
                        [anchors[2]['position']['x'], anchors[2]['position']['y'], anchors[2]['position']['z']]   # cart
                    ])
                
                    print("Unity Anchor Points:", unity_points, flush=True)


                    # CALIBRATION PHASE
                    if T_matrix is None:
                        # get the first three point (head, lHand, rHand) points from skeleton points
                        calibration_samples.append((skeleton_points.copy()[:3], unity_points.copy()))
                        print(f"calibration sample {len(calibration_samples)}/{CALIBRATION_SAMPLES_NEEDED}", flush=True)
                        
                        if len(calibration_samples) >= CALIBRATION_SAMPLES_NEEDED:
                            all_skeleton = np.vstack([s for s, u in calibration_samples]) # CALIBRATION_SAMPLES_NEEDED X 4
                            all_unity = np.vstack([u for s, u in calibration_samples]) # CALIBRATION_SAMPLES_NEEDED X 4

                            
                            skeleton_homogeneous = np.hstack([all_skeleton, np.ones((all_skeleton.shape[0], 1))])
                            
                            T_transpose, _, _, s = lstsq(skeleton_homogeneous, all_unity)
                            T_matrix = T_transpose.T
                            
                            print("=" * 50)
                            print("CALIBRATION COMPLETE!")
                            print("T_matrix:")
                            print(T_matrix)

                    # TRACKING PHASE
                    if T_matrix is not None:
                        transformed_skeleton_anchors = []
                        transformed_arcuo_anchors = []
                        
                        # transform skeleton points
                        for index, skeleton_point in enumerate(skeleton_points):
                            homogenous_skeleton_point = np.array([
                                skeleton_point[0], 
                                skeleton_point[1], 
                                skeleton_point[2], 
                                1.0
                            ])
                            
                            transformed_point = (T_matrix @ homogenous_skeleton_point)[:3]
                            
                            transformed_skeleton_anchor = {
                                'anchor_id': index,
                                'original_position': {
                                    'x': float(skeleton_point[0]),
                                    'y': float(skeleton_point[1]),
                                    'z': float(skeleton_point[2]),
                                },
                                'transformed_position': {
                                    'x': float(transformed_point[0]),
                                    'y': float(transformed_point[1]),
                                    'z': float(transformed_point[2]),
                                }
                            }
                            transformed_skeleton_anchors.append(transformed_skeleton_anchor)
                        
                        # transform skeleton points
                        if arcuo_coordinates:
                            for marker_id, marker_pos in arcuo_coordinates.items():

                                marker_point = np.array([marker_pos[0], marker_pos[1], marker_pos[2], 1.0])
                                transformed_point = (T_matrix @ marker_point)[:3]

                                transformed_arcuo_anchor ={
                                    'anchor_id': marker_id,
                                    'original_position': {
                                        'x': float(marker_pos[0]),
                                        'y': float(marker_pos[1]),
                                        'z': float(marker_pos[2])
                                    },
                                    'transformed_position': {
                                        'x': float(transformed_point[0]),
                                        'y': float(transformed_point[1]),
                                        'z': float(transformed_point[2])
                                    }
                                }

                                transformed_arcuo_anchors.append(transformed_arcuo_anchor)


                        # assemble message type
                        response_msg = {
                            'transformedSkeletonAnchors': transformed_skeleton_anchors,
                            'transformedArcuoAnchors': transformed_arcuo_anchors # transformed_arcuo_anchors
                        }
                        print(f"Sending transformed positions:", response_msg, flush=True)
                        send(sock, response_msg)
                        arcuo_coordinates = {}
                    else:
                        response_msg = {
                            'transformedSkeletonAnchors': [],
                            'transformedArcuoAnchors': []
                        }
                        send(sock, response_msg)
                        arcuo_coordinates = {}
                    
                    time.sleep(0.05)
                    
                except BlockingIOError:
                    pass
                except Exception as e:
                    print(f"ERROR: {e}", flush=True)
                
                # Display
                cv2.namedWindow('RealSense', cv2.WINDOW_AUTOSIZE)
                color_image = cv2.flip(color_image, 1)
                cv2.imshow('RealSense', color_image)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break
                    
    finally:
        pipeline.stop()
        cv2.destroyAllWindows()

def receive(sock):
    data = sock.recv(4096)
    data = data.decode('utf-8')
    msg = json.loads(data)
    # print("Received:", msg, flush=True)
    return msg

def send(sock, msg):
    data = json.dumps(msg)
    sock.sendall(data.encode('utf-8'))
    print("Sent to server:", msg, flush=True)

if __name__ == "__main__":
    # start thread with socket code
    # t1 = threading.Thread(target=socket_client)
    # t1.start()
    socket_client()

    # realsense runs on the main thread
    # realsense_loop()