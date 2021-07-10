using System;
using System.Drawing;
using System.Windows.Forms;

public class BebopButton : Button {
	public static int ButtonSize = 90;
	public BebopButton(Point location, String text, Font font) {
		Location = location;
		Text = text;

		//Constants
		BackColor = Color.FromArgb(215, 215, 215);
		FlatStyle = FlatStyle.System;
		Font = font;
		Size = new Size(ButtonSize, ButtonSize);
	}
}