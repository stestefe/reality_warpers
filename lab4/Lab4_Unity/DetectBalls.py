import cv2
from ultralytics import YOLO

model = YOLO("yolov8n.pt") 

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)  
# cap = cv2.VideoCapture(0)

if not cap.isOpened():
    exit()


SPORTS_BALL_CLASS = 32

while True:
    ret, frame = cap.read()
    if not ret:
        break

    results = model.predict(frame, verbose=False)

    for result in results:
        boxes = result.boxes
        for box in boxes:
            cls_id = int(box.cls[0])
            conf = float(box.conf[0])
            if cls_id == SPORTS_BALL_CLASS and conf > 0.3:  
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
                cv2.putText(frame, f"Ball {conf:.2f}", (x1, y1 - 5),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

    cv2.imshow("Ball Detection", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()