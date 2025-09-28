import socket
import json
import pyrealsense2 as rs
import numpy as np
import cv2
from collections import defaultdict
import threading
import time

# HOST = "192.168.0.115"
HOST = "127.0.0.1"   # localhost
PORT = 13456


id_coordinates = {}
lock = threading.Lock()

def socket_client():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.connect((HOST, PORT))
        while True:
            try:
                # data = sock.recv(1024)
                # if not data:
                #     break
                # data = data.decode('utf-8')
                # msg = json.loads(data)
                # print("Received from server:", msg)
                msg = receive(sock)
                print(id_coordinates)

                # thread safety
                with lock:
                    coords_to_send = id_coordinates.copy()

                # msg['some_string'] = "From Client"
                # msg['some_int'] -= 1
                msg['id_coordinates'] = coords_to_send
                
				# list of anchors 
                # now see which anchor is closes to which marker
                # the anchor will be assined to the nearest marker
                
				
                

                
                send(sock, msg)
                time.sleep(0.2)  # delay

            except Exception as e:
                print("Socket error:", e)
                break

def receive(sock):
    data = sock.recv(1024)
    data = data.decode('utf-8')
    msg = json.loads(data)
    print("Received: ", msg)
    return msg

def send(sock, msg):
	data = json.dumps(msg)
	sock.sendall(data.encode('utf-8'))
	print("Sent to server:", msg)


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
