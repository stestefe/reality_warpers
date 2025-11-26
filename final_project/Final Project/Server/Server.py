import asyncio
import json
from dataclasses import dataclass
from typing import List, Optional

# -------------------------------
# Data Classes
# -------------------------------

@dataclass
class Transform:
    position: List[float]  # [x, y, z]
    rotation: List[float]  # Quaternion [x, y, z, w]

@dataclass
class TransformMessage:
    type: str
    position: List[float]
    rotation: List[float]

@dataclass
class MessageBase:
    type: str
    payload: Optional[str] = None

# -------------------------------
# Client Connection
# -------------------------------

class ClientConnection:
    def __init__(self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
        self.reader = reader
        self.writer = writer
        self.last_transform: Optional[Transform] = None
        self.address = writer.get_extra_info('peername')
        asyncio.create_task(self.receive_loop())

    async def receive_loop(self):
        try:
            while True:
                data = await self.reader.readline()
                if not data:
                    break
                await self.handle_data(data)
        except Exception as e:
            print(f"[{self.address}] Error: {e}")
        finally:
            print(f"[{self.address}] Disconnected")
            self.writer.close()
            await self.writer.wait_closed()

    async def handle_data(self, data: bytes):
        try:
            messages = data.decode('utf-8').strip().split('\n')
            for msg_str in messages:
                msg_json = json.loads(msg_str)
                msg_type = msg_json.get("type")
                if msg_type == "FrustumTransform":
                    self.last_transform = Transform(
                        position=msg_json["position"],
                        rotation=msg_json["rotation"]
                    )
                    print(f"[{self.address}] Received transform: {self.last_transform}")
                else:
                    print(f"[{self.address}] Unknown message type: {msg_type}")
        except Exception as e:
            print(f"[{self.address}] Failed to decode message: {e}")

    async def send_message(self, message: dict):
        try:
            json_str = json.dumps(message) + "\n"
            self.writer.write(json_str.encode('utf-8'))
            await self.writer.drain()
        except Exception as e:
            print(f"[{self.address}] Failed to send message: {e}")

# -------------------------------
# TCP Server
# -------------------------------

class TCPServer:
    def __init__(self, host='0.0.0.0', port=13456, broadcast_interval=0.05):
        self.host = host
        self.port = port
        self.clients: list[ClientConnection] = []
        self.broadcast_interval = broadcast_interval

    async def handle_client(self, reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
        client = ClientConnection(reader, writer)
        self.clients.append(client)
        await writer.wait_closed()
        self.clients.remove(client)

    async def broadcast_loop(self):
        while True:
            await asyncio.sleep(self.broadcast_interval)
            for client in self.clients:
                if client.last_transform:
                    message = {
                        "type": "FrustumTransform",
                        "position": client.last_transform.position,
                        "rotation": client.last_transform.rotation
                    }
                    print(f"Sent: {message}")
                    # Broadcast to all clients
                    for other_client in self.clients:
                        await other_client.send_message(message)

    async def start(self):
        server = await asyncio.start_server(self.handle_client, self.host, self.port)
        print(f"Server started on {self.host}:{self.port}")
        async with server:
            await asyncio.gather(
                server.serve_forever(),
                self.broadcast_loop()
            )

# -------------------------------
# Run Server
# -------------------------------

if __name__ == "__main__": 
    server = TCPServer()
    asyncio.run(server.start())
