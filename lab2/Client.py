import socket
import json
import pyrealsense2 as rs
import numpy as np
import cv2
from collections import defaultdict
import threading
import time

from scipy.linalg import lstsq

# HOST = "192.168.0.115"
HOST = "127.0.0.1"   # localhost
PORT = 13456


id_coordinates = {}
lock = threading.Lock()

def socket_client():
    T_matrix = None
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.connect((HOST, PORT))
        while True:
            try:
                msg = receive(sock)

                # thread safety
                with lock:
                    coords_to_send = id_coordinates.copy()

                print("sanchors", msg['listOfAnchors'], flush= True)
                anchors = msg['listOfAnchors']
                sorted_anchors = sorted(anchors, key=lambda x: x['id'])
                quest_ids = [anchor['id'] for anchor in sorted_anchors]

                quest_points = np.array([
                    [anchor['position']['x'], anchor['position']['y'], anchor['position']['z']]
                    for anchor in sorted_anchors
                ])

                print("quest points", quest_points, flush = True)
                print("quest ids:", quest_ids, flush = True)
            
                marker_points_list = []
                for quest_id in quest_ids:
                    if quest_id in id_coordinates:
                        marker_points_list.append(id_coordinates[quest_id])
                    else:
                        print(f"WARNING: quest id {quest_id} not found in marker points")
                        marker_points_list = []
                        break
                
                if not marker_points_list:
                    continue

                marker_points = np.array(marker_points_list)
                
                print("marker points", marker_points, flush = True)
                print("marker points dict", id_coordinates, flush = True)

                if quest_points.shape[0] == marker_points.shape[0]:
                    quest_homogeneous = np.hstack([quest_points, np.ones((quest_points.shape[0], 1))])
                    marker_homogeneous = np.hstack([marker_points, np.ones((marker_points.shape[0], 1))])

                    # Calibration: Calculate transformation matrix once
                    if T_matrix is None and len(quest_points) == len(marker_points) and len(quest_points) >= 3:
                        A = marker_homogeneous
                        b = quest_homogeneous

                        T_transpose, _, _, _ = lstsq(A, b)
                        T_matrix = T_transpose.T
                        print("---------------------------------------------------TRANSFORMATION MATRIX:\n", T_matrix, flush=True)

                    if T_matrix is not None:
                        transformed_homogeneous = (T_matrix @ marker_homogeneous.T).T
                        transformed_points = transformed_homogeneous[:, :3]
                        
                        transformed_anchors = []
                        for i, quest_id in enumerate(quest_ids):
                            transformed_anchor = {
                                'anchor_id': quest_id,
                                # 'marker_id': quest_id,
                                'original_position': {
                                    'x': float(marker_points[i][0]),
                                    'y': float(marker_points[i][1]),
                                    'z': float(marker_points[i][2])
                                },
                                'transformed_position': {
                                    'x': float(transformed_points[i][0]),
                                    'y': float(transformed_points[i][1]),
                                    'z': float(transformed_points[i][2])
                                }
                            }
                            transformed_anchors.append(transformed_anchor)
                        
                        response_msg = {
                            'transformedAnchors': transformed_anchors
                        }
                        
                        print("Sending transformed anchors:", response_msg, flush = True)
                        send(sock, response_msg)

                time.sleep(0.2)  # delay

            except Exception as e:
                print("Socket error:", e, flush = True)
                break

def receive(sock):
    data = sock.recv(1024)
    data = data.decode('utf-8')
    msg = json.loads(data)
    print("Received: ", msg , flush = True)
    return msg

def send(sock, msg):
    data = json.dumps(msg)
    sock.sendall(data.encode('utf-8'))
    print("Sent to server:", msg, flush = True)


# copied from lab1
def realsense_loop():
    global id_coordinates
    pipeline = rs.pipeline()
    config = rs.config()

    pipeline_wrapper = rs.pipeline_wrapper(pipeline)
    pipeline_profile = config.resolve(pipeline_wrapper)
    device = pipeline_profile.get_device()

    config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
    config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)

    arucoDict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_1000)
    arucoParam = cv2.aruco.DetectorParameters()
    arucoDetector = cv2.aruco.ArucoDetector(arucoDict, arucoParam)

    pipeline.start(config)

    try:
        while True:
            frames = pipeline.wait_for_frames()
            depth_frame = frames.get_depth_frame()
            color_frame = frames.get_color_frame()
            if not depth_frame or not color_frame:
                continue

            depth_image = np.asanyarray(depth_frame.get_data())
            color_image = np.asanyarray(color_frame.get_data())

            corners, ids, _ = arucoDetector.detectMarkers(color_image)
            intrinsics = depth_frame.profile.as_video_stream_profile().get_intrinsics()

            local_coordinates = {}

            if ids is not None:
                for id, marker in zip(ids, corners):
                    x, y = marker[0][0]  # top-left corner
                    depth = depth_frame.get_distance(int(x), int(y))
                    coord = rs.rs2_deproject_pixel_to_point(intrinsics, [x, y], depth)
                    local_coordinates[int(id[0])] = coord

                # update thread safety
                with lock:
                    id_coordinates = local_coordinates.copy()

                color_image = cv2.aruco.drawDetectedMarkers(color_image, corners, ids)

            depth_colormap = cv2.applyColorMap(cv2.convertScaleAbs(depth_image, alpha=0.03), cv2.COLORMAP_JET)
            images = np.hstack((color_image, depth_colormap))
            cv2.imshow('RealSense', images)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

    finally:
        pipeline.stop()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    # start thead with socket code
    t1 = threading.Thread(target=socket_client)
    t1.start()

    # realsense runs on the main thread
    realsense_loop()
