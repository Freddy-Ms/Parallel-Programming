import socket
from utils import send_all, receive_all, sobel_filter

def client_main():
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client_socket.connect(('127.0.0.1', 2040))  # IP serwera

    fragment = receive_all(client_socket)

    processed_fragment = sobel_filter(fragment)

    send_all(client_socket, processed_fragment)
    client_socket.close()
    print("Fragment przetworzony i wyslany z powrotem do serwera")

if __name__ == "__main__":
    client_main()
