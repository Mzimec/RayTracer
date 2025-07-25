// See https://aka.ms/new-console-template for more information


using System.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

int size = 16;
string outputFile = "CheckerTexture.png";
Rgb24[,] image = new Rgb24[size, size];

for (int i = 0; i < size; i++) {
    for (int j = 0; j < size; j++) {
        int row = i / 8;
        int col = j / 8;
        image[i, j] = (row + col) % 2 == 0 ? new Rgb24(100,100,100) : new Rgb24(140,140,140); // Checkerboard pattern
    }
}

using (var img = new Image<Rgba32>(size, size)) {
    for (int i = 0; i < size; i++) {
        for (int j = 0; j < size; j++) {
            img[i, j] = new Rgba32(image[i, j].R, image[i, j].G, image[i, j].B);
        }
    }
    img.Save(outputFile);
}


