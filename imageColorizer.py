import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk, ImageOps


class ImageColorizerApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Image Colorizer")

        # Variables
        self.original_image = None
        self.modified_image = None
        self.base_image = None
        self.masked_base_image = None

        # Layout
        self.create_ui()

    def create_ui(self):
        # Panel 1: Original Image
        self.original_panel = tk.Label(self.root, text="Original Image", bg="gray")
        self.original_panel.grid(row=0, column=0, padx=10, pady=10)

        # Panel 2: Modified Mask Image
        self.modified_panel = tk.Label(self.root, text="Mask Image", bg="gray")
        self.modified_panel.grid(row=0, column=1, padx=10, pady=10)

        # Panel 3: Base Image with Mask Applied
        self.base_panel = tk.Label(self.root, text="Base Image", bg="gray")
        self.base_panel.grid(row=0, column=2, padx=10, pady=10)

        # Controls
        self.controls_frame = tk.Frame(self.root)
        self.controls_frame.grid(row=1, column=0, columnspan=3, pady=10)

        # Sliders for RGB values
        self.red_slider = self.create_slider("Red", 0, self.controls_frame)
        self.green_slider = self.create_slider("Green", 1, self.controls_frame)
        self.blue_slider = self.create_slider("Blue", 2, self.controls_frame)

        # Buttons
        self.load_button = tk.Button(self.controls_frame, text="Load Grayscale Image", command=self.load_image)
        self.load_button.grid(row=3, column=0, pady=5)

        self.load_base_button = tk.Button(self.controls_frame, text="Load Base Image", command=self.load_base_image)
        self.load_base_button.grid(row=3, column=1, pady=5)

        self.save_mask_button = tk.Button(self.controls_frame, text="Save Mask", command=self.save_mask, state=tk.DISABLED)
        self.save_mask_button.grid(row=3, column=2, pady=5)

        self.apply_mask_button = tk.Button(self.controls_frame, text="Apply Mask", command=self.apply_mask, state=tk.DISABLED)
        self.apply_mask_button.grid(row=4, column=0, pady=5)

        self.save_masked_base_button = tk.Button(self.controls_frame, text="Save Masked Base", command=self.save_masked_base, state=tk.DISABLED)
        self.save_masked_base_button.grid(row=4, column=1, pady=5)

    def create_slider(self, label, row, parent):
        tk.Label(parent, text=label).grid(row=row, column=0, padx=5, sticky="e")
        slider = tk.Scale(parent, from_=0, to=255, orient="horizontal", command=self.update_image)
        slider.grid(row=row, column=1, padx=5)
        slider.set(128)  # Default midpoint
        return slider

    def load_image(self):
        file_path = filedialog.askopenfilename(
            filetypes=[("Image Files", "*.png;*.jpg;*.jpeg"), ("All Files", "*.*")]
        )
        if file_path:
            self.original_image = Image.open(file_path).convert("LA")  # Load grayscale with alpha
            self.display_image(self.original_image, self.original_panel)

            # Initialize modified mask
            self.modified_image = self.original_image.copy()
            self.update_image()

            # Enable save mask button
            self.save_mask_button.config(state=tk.NORMAL)

    def load_base_image(self):
        file_path = filedialog.askopenfilename(
            filetypes=[("Image Files", "*.png;*.jpg;*.jpeg"), ("All Files", "*.*")]
        )
        if file_path:
            self.base_image = Image.open(file_path).convert("RGBA")
            self.display_image(self.base_image, self.base_panel)

            # Enable apply mask button if the mask is ready
            if self.modified_image:
                self.apply_mask_button.config(state=tk.NORMAL)

    def display_image(self, image, panel):
        # Ensure panel size matches image size
        panel.config(width=image.width, height=image.height)
        img_tk = ImageTk.PhotoImage(image)
        panel.config(image=img_tk)
        panel.image = img_tk

    def update_image(self, *args):
        if self.original_image is None:
            return

        r = self.red_slider.get()
        g = self.green_slider.get()
        b = self.blue_slider.get()

        gray, alpha = self.original_image.split()
        colorized = ImageOps.colorize(gray, black=(0, 0, 0), white=(r, g, b))
        colorized.putalpha(alpha)  # Preserve original alpha channel

        self.modified_image = colorized
        self.display_image(self.modified_image, self.modified_panel)

        # Enable apply mask button if base image is loaded
        if self.base_image:
            self.apply_mask_button.config(state=tk.NORMAL)

    def apply_mask(self):
        if not self.base_image or not self.modified_image:
            return

        base_r, base_g, base_b, base_alpha = self.base_image.split()
        mask_r, mask_g, mask_b, mask_alpha = self.modified_image.split()

        # Apply mask: Combine mask RGB with base alpha
        masked_r = Image.composite(mask_r, base_r, mask_alpha)
        masked_g = Image.composite(mask_g, base_g, mask_alpha)
        masked_b = Image.composite(mask_b, base_b, mask_alpha)

        # Recombine channels into a final image
        self.masked_base_image = Image.merge("RGBA", (masked_r, masked_g, masked_b, base_alpha))
        self.display_image(self.masked_base_image, self.base_panel)

        self.save_masked_base_button.config(state=tk.NORMAL)

    def save_mask(self):
        if self.modified_image:
            save_path = "colorized_mask_image.png"
            self.modified_image.save(save_path, format="PNG")
            messagebox.showinfo("Saved", f"Mask saved as {save_path}")

    def save_masked_base(self):
        if self.masked_base_image:
            save_path = "masked_base_image.png"
            self.masked_base_image.save(save_path, format="PNG")
            messagebox.showinfo("Saved", f"Masked base image saved as {save_path}")


if __name__ == "__main__":
    root = tk.Tk()
    app = ImageColorizerApp(root)
    root.mainloop()
