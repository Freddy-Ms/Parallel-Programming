import pickle
import socket
import numpy as np
from PIL import Image


# ------------------- KOMUNIKACJA -------------------

def send_all(sock, data):
    data = pickle.dumps(data)
    sock.sendall(len(data).to_bytes(4, byteorder='big'))
    sock.sendall(data)


def receive_all(sock):
    length = int.from_bytes(sock.recv(4), byteorder='big')
    data = b''
    while len(data) < length:
        packet = sock.recv(4096)
        if not packet:
            break
        data += packet
    return pickle.loads(data)


# ------------------- PRZETWARZANIE OBRAZU -------------------

def sobel_filter(image_array):
    Kx = np.array([[1, 0, -1],
                   [2, 0, -2],
                   [1, 0, -1]])
    Ky = np.array([[1, 2, 1],
                   [0, 0, 0],
                   [-1, -2, -1]])

    if image_array.ndim == 3:
        image_array = np.dot(image_array[..., :3], [0.299, 0.587, 0.114])

    rows, cols = image_array.shape
    output = np.zeros_like(image_array)

    for i in range(1, rows - 1):
        for j in range(1, cols - 1):
            gx = np.sum(Kx * image_array[i - 1:i + 2, j - 1:j + 2])
            gy = np.sum(Ky * image_array[i - 1:i + 2, j - 1:j + 2])
            output[i, j] = np.sqrt(gx ** 2 + gy ** 2)

    output = (output / output.max()) * 255
    return output.astype(np.uint8)


# ------------------- DZIELENIE / SCALANIE -------------------

def split_image(image, n_parts):
    arr = np.array(image)
    h = arr.shape[0]
    chunk_h = h // n_parts
    parts = []
    for i in range(n_parts):
        start = i * chunk_h
        end = (i + 1) * chunk_h if i < n_parts - 1 else h
        parts.append(arr[start:end, :])
    return parts


def merge_image(parts):
    combined = np.vstack(parts)
    return Image.fromarray(combined)
