using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using NLua;

public class MainForm : Form {
	public const int WM_HOTKEY_MSG_ID = 0x0312;
	//Font settings
	private const int FontSize = 11;
	private const String FontName = "Segoe UI";

	private Font font;

	//Controls
	private BebopButton playButton;
	private BebopButton pauseButton;
	private BebopButton skipButton;
	private ComboBox playlistComboBox;
	private TrackBar volumeSlider;
	private Label statusLabel;
	private CheckBox shuffleCheckBox;

	//Key hooks
	KeyHandler playPauseHandler;
	KeyHandler skipHandler;
	KeyHandler volumeUpHandler;
	KeyHandler volumeDownHandler;

	MusicPlayer musicPlayer;
	private string currentSongFilename;

	private Random rand = new Random();

	Lua state;

	//The currently playing song
	int playlistCounter = -1;
	private String[] playlist;
	private string currentPlaylistFile;
	//This NEEDS to end in a slash
#if DEBUG
	const string playlistFolder = "C:\\Users\\carso\\Documents\\ProgrammingProjects\\C#\\BebopPlayerV3\\playlists\\";
#else
	const string playlistFolder = "./playlists/";
#endif
	bool weStoppedTheSong = false;

	public MainForm() {
		//Set up lua state
		state = new Lua();
		state.LoadCLRPackage();
		
		//Things for this window
		this.Size = new Size(600, 175);
		this.Text = "Bebop Music Player 3.0";
		BackColor = Color.FromArgb(171, 171, 171);
		
		//Buttons and other controls

		font = new Font(new FontFamily(FontName), FontSize);

		playButton = new BebopButton(new Point(0,0), "Play", font);
		pauseButton = new BebopButton(new Point(BebopButton.ButtonSize * 1, 0), "Pause", font);
		skipButton = new BebopButton(new Point(BebopButton.ButtonSize * 2, 0), "Skip", font);

		playButton.Click += onPlayButtonClick;
		pauseButton.Click += onPauseButtonClick;
		skipButton.Click += onSkipButtonClick;

		//playlist list box
		playlistComboBox = new ComboBox();
		//Iterate over lua files
		playlistComboBox.Items.AddRange(Directory.GetFiles(playlistFolder));
		for (int q = 0; q < playlistComboBox.Items.Count; q++) {
			playlistComboBox.Items[q] = Path.GetFileNameWithoutExtension(playlistComboBox.Items[q].ToString());
		}

		playlistComboBox.Location = new Point(BebopButton.ButtonSize * 3, 0);
		playlistComboBox.SelectedValueChanged += onPlaylistComboBoxSelectionChanged;

		//Volume slider
		volumeSlider = new TrackBar();
		volumeSlider.Location = new Point(0, BebopButton.ButtonSize);
		volumeSlider.Size = new Size(BebopButton.ButtonSize + (BebopButton.ButtonSize / 2), 24);
		volumeSlider.TickStyle = TickStyle.None;
		volumeSlider.Minimum = 0;
		volumeSlider.Maximum = 100;
		volumeSlider.Value = 30;
		volumeSlider.ValueChanged += onVolumeSliderValueChanged;

		//Status label
		statusLabel = new Label();
		statusLabel.Location = new Point(volumeSlider.Size.Width, volumeSlider.Location.Y);
		statusLabel.Size = new Size(1000, volumeSlider.Size.Height);
		statusLabel.Font = font;

		//Shuffle check box
		shuffleCheckBox = new CheckBox();
		shuffleCheckBox.Location = new Point(
			playlistComboBox.Location.X + playlistComboBox.Size.Width,
			playlistComboBox.Location.Y
		);
		shuffleCheckBox.Checked = true;

		
		Controls.Add(playButton);
		Controls.Add(pauseButton);
		Controls.Add(skipButton);
		Controls.Add(playlistComboBox);
		Controls.Add(volumeSlider);
		Controls.Add(statusLabel);
		Controls.Add(shuffleCheckBox);

		//Set up the music player
		musicPlayer = new MusicPlayer();
		musicPlayer.setOnPlaybackStopped(onPlaybackStopped);
		setVolumeBasedOnSliderValue();
		loadPlaylistByIdx(0);
		playNextSong();

		updateStatusLabel();

		//Hotkeys
		playPauseHandler = new KeyHandler(Keys.F8, this);
		playPauseHandler.Register();

		skipHandler = new KeyHandler(Keys.F9, this);
		skipHandler.Register();

		volumeUpHandler = new KeyHandler(Keys.F11, this);
		volumeUpHandler.Register();

		volumeDownHandler = new KeyHandler(Keys.F10, this);
		volumeDownHandler.Register();
	}

	protected override void WndProc(ref Message m) {
		//This is a hotkey
		if (m.Msg == WM_HOTKEY_MSG_ID) {
			if (m.WParam.ToInt32() == playPauseHandler.GetHashCode()) {
				HandlePlayPauseHotkey();
			}
			if (m.WParam.ToInt32() == skipHandler.GetHashCode()) {
				HandleSkipHotkey();
			}
			if (m.WParam.ToInt32() == volumeUpHandler.GetHashCode()) {
				HandleVolumeUpHotkey();
			}
			if (m.WParam.ToInt32() == volumeDownHandler.GetHashCode()) {
				HandleVolumeDownHotkey();
			}
		}
		base.WndProc(ref m);
	}
	//Hotkey handler functions
	private void HandlePlayPauseHotkey() {
		if (musicPlayer.isPlaying()) {
			musicPlayer.pause();
		}
		else {
			musicPlayer.resume();
		}
	}
	private void HandleSkipHotkey() {
		playNextSong();
	}

	//The amount the volume slider changes when you press a hotkey
	const int volumeHotkeyChange = 2;
	private void HandleVolumeUpHotkey() {
		if (volumeSlider.Value + volumeHotkeyChange <= volumeSlider.Maximum)
			volumeSlider.Value += volumeHotkeyChange;
	}
	private void HandleVolumeDownHotkey() {
		if (volumeSlider.Value - volumeHotkeyChange >= volumeSlider.Minimum)
			volumeSlider.Value -= volumeHotkeyChange;
	}

	//Utility functions
	private void updateStatusLabel() {
		if (musicPlayer.isPlaying()) {
			statusLabel.Text = "Now Playing: " + Path.GetFileNameWithoutExtension(currentSongFilename);
		}
		else {
			statusLabel.Text = "Now Playing: Paused";
		}
	}
	private void loadPlaylistByFilename(string filename) {
		//Take the ordered list and shuffle it
		List<string> orderedList = new List<string>();
		orderedList.AddRange(state.DoFile(filename)[0] as string[]);

		playlist = new string[orderedList.Count];
		currentPlaylistFile = filename;

		for (int q = 0; q < playlist.Length; q++) {
			int n;
			if (shuffleCheckBox.Checked)
				n = rand.Next(orderedList.Count);
			else
				n = 0;
			playlist[q] = orderedList[n];
			orderedList.RemoveAt(n);
		}
		playlistCounter = -1;
	}
	private void playNextSong() {
		playlistCounter++;
		if (playlistCounter >= playlist.Length) {
			loadPlaylistByFilename(currentPlaylistFile);
		}
		//Weird bit to handle the fact that the very first song does not get stopped at all
		//Messes up the whole thing with the weStoppedTheSongVariable
		weStoppedTheSong = !musicPlayer.isStopped();
		currentSongFilename = playlist[playlistCounter];
		musicPlayer.play(currentSongFilename);
		updateStatusLabel();
	}
	private string playlistNameToFilename(string name) {
		return playlistFolder + name + ".lua";
	}
	private void loadPlaylistByIdx(int idx) {
		//Get the name and then just load by name
		playlistComboBox.SelectedIndex = idx;
		loadPlaylistByFilename(playlistNameToFilename(playlistComboBox.Items[idx].ToString()));
	}

	//Button Events
	private void onPlayButtonClick(object sender, EventArgs eventArgs) {
		musicPlayer.resume();
	}
	private void onPauseButtonClick(object sender, EventArgs eventArgs) {
		musicPlayer.pause();
	}
	private void onSkipButtonClick(object sender, EventArgs eventArgs) {
		playNextSong();
	}
	private void onPlaybackStopped(object sender, EventArgs eventArgs) {
		if (!weStoppedTheSong)
			playNextSong();
		weStoppedTheSong = false;
	}
	//ComboBox events
	private void onPlaylistComboBoxSelectionChanged(object sender, EventArgs eventArgs) {
		loadPlaylistByFilename(playlistNameToFilename(playlistComboBox.SelectedItem.ToString()));
		playNextSong();
	}

	private void onVolumeSliderValueChanged(object sender, EventArgs eventArgs) {
		setVolumeBasedOnSliderValue();
	}

	private void setVolumeBasedOnSliderValue() {
		float newValue = MathF.Pow(volumeSlider.Value / (float)volumeSlider.Maximum, 1.5f);
		musicPlayer.setVolume(newValue);
	}


	[STAThread]
	public static void Main(string[] args) {
		Application.EnableVisualStyles();
		Application.Run(new MainForm());
		
	}
}
