import socket
from PIL import Image
from io import BytesIO

def send_image(sock, img):
    buf = BytesIO()
    img.save(buf, format="PNG")
    data = buf.getvalue()
    sock.sendall(len(data).to_bytes(4, 'big'))
    sock.sendall(data)

def recv_image(sock):
    length = int.from_bytes(sock.recv(4), 'big')
    data = b""
    while len(data) < length:
        packet = sock.recv(4096)
        if not packet:
            break
        data += packet
    return Image.open(BytesIO(data))

def split_image(image, n):
    w, h = image.size
    step = h // n
    parts = []
    for i in range(n):
        y1 = i * step
        y2 = (i+1)*step if i < n-1 else h
        parts.append(image.crop((0, y1, w, y2)))
    return parts

def merge_image(parts):
    w = parts[0].size[0]
    h = sum(p.size[1] for p in parts)
    img = Image.new("RGB", (w, h))
    y = 0
    for p in parts:
        img.paste(p, (0, y))
        y += p.size[1]
    return img

def server_main(image_path, n_clients):
    image = Image.open(image_path)
    fragments = split_image(image, n_clients)

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind(("127.0.0.1", 2040))
    server.listen(n_clients)
    print("Serwer nasluchuje...")

    processed = []
    for i in range(n_clients):
        client, addr = server.accept()
        print("Polaczono z", addr)
        send_image(client, fragments[i])
        result = recv_image(client)
        processed.append(result)
        client.close()

    output = merge_image(processed)
    output.save("processed.png")
    print("Zapisano processed.png")

server_main("Doge.png", 1)  # liczba klientow
