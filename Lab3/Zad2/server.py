import socket
from PIL import Image
from utils import send_all, receive_all, split_image, merge_image

def server_main(image_path, n_clients):
    image = Image.open(image_path)
    fragments = split_image(image, n_clients)

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('127.0.0.1', 2040))  # wlasny adres IP
    server_socket.listen(n_clients)
    print("Serwer nasluchuje...")

    processed_fragments = []

    for i in range(n_clients):
        client_socket, client_address = server_socket.accept()
        print(f"Polaczono z klientem {i+1}: {client_address}")

        send_all(client_socket, fragments[i])

        processed_fragment = receive_all(client_socket)
        processed_fragments.append(processed_fragment)

        client_socket.close()

    result_image = merge_image(processed_fragments)
    result_image.save("processed_image.png")
    print("Obraz przetworzony zapisany jako processed_image.png")

if __name__ == "__main__":
    server_main("banan.png", 1)  # 2 - liczba klientow
