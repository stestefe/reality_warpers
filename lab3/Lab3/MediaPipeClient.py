import socket
import json
import cv2
import numpy as np
import pyrealsense2 as rs
from MediaPipe import MediaPipe
import socket
import json
import pyrealsense2 as rs
import numpy as np
import cv2
from collections import defaultdict, deque
import threading
import time

from scipy.linalg import lstsq

'''The server's hostname or IP address'''
HOST = "127.0.0.1" 
'''The port used by the server'''
PORT = 13456

def main():
    mp = MediaPipe()
    
    # Configure depth and color streams
    pipeline = rs.pipeline()
    config = rs.config()

    # Get device product line for setting a supporting resolution
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
   

    # Start streaming
    pipeline.start(config)
    # Align Color and Depth
    align_to = rs.stream.color
    align = rs.align(align_to)
    body_dict = {'head' : 0, 'leftHand': 1, 'rightHand': 2}

    T_matrix = None
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
            sock.connect((HOST, PORT))
            sock.setblocking(0)
            while True:
                # Wait for a coherent pair of frames: depth and color
                frames = pipeline.wait_for_frames()
                aligned_frames = align.process(frames)
                depth_frame = aligned_frames.get_depth_frame()
                color_frame = aligned_frames.get_color_frame()
                if not  depth_frame or not color_frame:
                    continue


                # Detect skeleton and send it to Unity
                color_image = np.asanyarray(color_frame.get_data())
                detection_results = mp.detect(color_image)
                color_image = mp.draw_landmarks_on_image(color_image, detection_results)
                # SKELETON DATA {'LHand_x': -0.17059844732284546, 'LHand_y': 1.2849622815847397, 'LHand_z': -0.6300000548362732, 'RHand_x': 0.23544804751873016, 'RHand_y': 1.3979488387703896, 'RHand_z': -0.6070000529289246, 'Head_x': 0.025743409991264343, 'Head_y': 1.6948225647211075, 'Head_z': -0.625}
                skeleton_data = mp.skeleton(color_image, detection_results, depth_frame)
                if skeleton_data is None:
                    continue
                
                skeleton_points = [
                    [skeleton_data['Head_x'], skeleton_data['Head_y'], skeleton_data['Head_z']],
                    [skeleton_data['LHand_x'], skeleton_data['LHand_y'], skeleton_data['LHand_z']],
                    [skeleton_data['RHand_x'], skeleton_data['RHand_y'], skeleton_data['RHand_z']]
                ]
                # if skeleton_data is not None and :
                #      send(sock, skeleton_data)
                # JSON => head, leftHand, rightHand

                print("SKELETON DATA", skeleton_data, flush = True)
                try:
                    msg = receive(sock)
                    anchors = msg['listOfAnchors']
                    mediapipe_points = np.array([
                        [anchor['position']['x'], anchor['position']['y'], anchor['position']['z']]
                        for anchor in anchors
                    ])

                    print("quest points:", mediapipe_points, flush=True)

                    # CALIBRATION PHASE
                    if T_matrix is None:
                        if len(mediapipe_points) == len(anchors):
                            skeleton_homogeneous = np.hstack([skeleton_points, np.ones((skeleton_points.shape[0], 1))])
                            mediapipe_homogenous = np.hstack([mediapipe_points, np.ones((mediapipe_points.shape[0], 1))])

                            A = skeleton_homogeneous
                            b = mediapipe_homogenous

                            T_transpose, _, _, _ = lstsq(A,b)
                            T_matrix = T_transpose.T
                            print("---TRANSFORMATION MATRIX CALCULATED---\n", T_matrix, flush=True)

                    if T_matrix is not None:
                        transformed_anchors = []
                        for index, mediapipe_point in enumerate(mediapipe_points):
                            homogenous_mediapipe_point = np.array([mediapipe_point[0], mediapipe_point[1], mediapipe_point[2], 1.0])
                            transformed_point = (T_matrix @ homogenous_mediapipe_point)[:3]

                            transformed_anchor = {
                                'anchor_id': index,
                                'original_position': {
                                    'x' : float(mediapipe_point[0]),
                                    'y' : float(mediapipe_point[1]),
                                    'z' : float(mediapipe_point[2]),
                                },
                                'transformed_position' : {
                                    'x' : float(transformed_point[0]),
                                    'y' : float(transformed_point[1]),
                                    'z' : float(transformed_point[2]),
                                }
                            }

                            transformed_anchors.append(transformed_anchor)
                            response_msg = {
                                transformed_anchors.append(transformed_anchor)
                            }
                            print(f"Sending {len(transformed_anchors)} visible mediepipe landmarks:", response_msg, flush=True)
                            send(sock, response_msg)
                    else:
                        response_msg = {
                            'transformedAnchors': []
                        }
                        print("No markers visible - sending empty message", flush=True)
                        send(sock, response_msg)
                    time.sleep(0.2)
                except:
                    print("----------------ERROR has occured----------------")
                    pass
                # Show images
                cv2.namedWindow('RealSense', cv2.WINDOW_AUTOSIZE)
                color_image = cv2.flip(color_image,1)
                cv2.imshow('RealSense', color_image)
                cv2.waitKey(1)
    finally:
        # Stop streaming
        pipeline.stop()

def receive(sock):
    data = sock.recv(1024)
    data = data.decode('utf-8')
    msg = json.loads(data)
    print("Received:", msg, flush=True)
    return msg

def send(sock, msg):
	data = json.dumps(msg)
	sock.sendall(data.encode('utf-8'))
	print("Sent: ", msg)

if __name__ == '__main__':
    main()