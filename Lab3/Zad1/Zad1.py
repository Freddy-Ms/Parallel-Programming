from multiprocessing import Pool
from PIL import Image
import numpy as np


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
            output[i, j] = np.sqrt(gx**2 + gy**2)

    output = (output / output.max()) * 255
    return output.astype(np.uint8)


def process_chunk(chunk_data):
    return sobel_filter(chunk_data)


def split_image(image_array, num_chunks):
    height = image_array.shape[0]
    chunk_height = height // num_chunks
    chunks = []
    for i in range(num_chunks):
        start = i * chunk_height
        end = (i + 1) * chunk_height if i < num_chunks - 1 else height
        chunks.append(image_array[start:end, :])
    return chunks


def merge_chunks(chunks):
    return np.vstack(chunks)


if __name__ == "__main__":
    input_path = "banan.jpg"
    output_path = "output_sobel.jpg"

    image = Image.open(input_path)
    image_array = np.array(image)

    num_processes = 4
    chunks = split_image(image_array, num_processes)

    with Pool(num_processes) as pool:
        processed_chunks = pool.map(process_chunk, chunks)

    final_image = merge_chunks(processed_chunks)

    Image.fromarray(final_image).save(output_path)
    print(f"Zapisano wynikowy obraz: {output_path}")

