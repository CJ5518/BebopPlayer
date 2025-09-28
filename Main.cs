using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using NLua;



public class MainForm : Form {
	public const int WM_HOTKEY_MSG_ID = 0x0312;
	//Font settings
	private const int FontSize = 11;
	private const String FontName = "Segoe UI";

	private const string version = "3.4.1";

	//The number of milliseconds you have to double click the delete button
	const int deleteButtonDelayMillis = 250;
	//The minimum number of milliseconds between song skips with the skip hotkey
	const long skipButtonInterval = 50;

	private Font font;

	//Controls
	private BebopButton playButton;
	private BebopButton pauseButton;
	private BebopButton skipButton;
	private BebopButton deleteButton;

	private Label deleteLabel;
	private ComboBox[] playlistComboBoxes;
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

	Lua luaState;
	LuaTable playlistTable;

	//The currently playing song
	int playlistCounter = -1;
	private String[] playlist;
	List<String> fileDialogSelection;
	
	//Playlist MUST end in a slash
#if DEBUG
	const string playlistFolder = "C:\\Users\\carso\\Documents\\ProgrammingProjects\\C#\\BebopPlayerV3\\playlists\\";
#else
	const string playlistFolder = "./playlists/";
#endif
	bool weStoppedTheSong = false;

	public MainForm() {
#if DEBUG
		AllocConsole();
#endif
		//Set up lua state
		luaState = new Lua();
		luaState.LoadCLRPackage();

		//Things for this window
		this.Size = new Size(600, 175);
		this.Text = "Bebop Music Player v" + version;
		this.Icon = BebopPlayerV3.Properties.Resources.Icon;

		BackColor = Color.FromArgb(171, 171, 171);

		//Buttons and other controls

		font = new Font(new FontFamily(FontName), FontSize);
		Font slightlySmallerFont = new Font(new FontFamily(FontName), FontSize - 1);

		playButton = new BebopButton(new Point(0,0), "Play", font);
		pauseButton = new BebopButton(new Point(BebopButton.ButtonSize * 1, 0), "Pause", font);
		skipButton = new BebopButton(new Point(BebopButton.ButtonSize * 2, 0), "Skip", font);

		//Add click handlers
		playButton.Click += onPlayButtonClick;
		pauseButton.Click += onPauseButtonClick;
		skipButton.Click += onSkipButtonClick;

		//Create the combo boxes
		playlistComboBoxes = new ComboBox[3];
		for (int q = 0; q < 3; q++) {
			playlistComboBoxes[q] = new ComboBox();
			playlistComboBoxes[q].Location = new Point(BebopButton.ButtonSize * 3, q * (playlistComboBoxes[q].Size.Height + 3));
			playlistComboBoxes[q].Size = new Size(175, playlistComboBoxes[q].Size.Height);
			this.Controls.Add(playlistComboBoxes[q]);
		}
		playlistComboBoxes[0].SelectedIndexChanged += new EventHandler(playlistComboxBoxSelectionId0);
		playlistComboBoxes[1].SelectedIndexChanged += new EventHandler(playlistComboxBoxSelectionId1);
		playlistComboBoxes[2].SelectedIndexChanged += new EventHandler(playlistComboxBoxSelectionId2);
		//Set up the main combo box
		playlistTable = luaState.DoFile(playlistFolder + "main.lua")[0] as LuaTable;
		populateComboBox(playlistTable, playlistComboBoxes[0]);
		playlistComboBoxes[0].Items.Add("From files");

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
			playlistComboBoxes[0].Location.X + playlistComboBoxes[0].Size.Width,
			playlistComboBoxes[0].Location.Y
		);
		shuffleCheckBox.Size = new Size(15, 15);
		shuffleCheckBox.Checked = true;

		//Delete button
		deleteButton = new BebopButton(playlistComboBoxes[0].Location, "Delete File", slightlySmallerFont);
		deleteButton.Size = deleteButton.Size / 2;
		deleteButton.Location =
			new Point(deleteButton.Location.X + (playlistComboBoxes[0].Width * 5 / 4), BebopButton.ButtonSize / 6);

		deleteButton.Click += onDeleteButtonClick;

		//Delete label
		deleteLabel = new Label();
		deleteLabel.Location = new Point(
			deleteButton.Location.X,
			deleteButton.Location.Y + deleteButton.Size.Height
		);
		deleteLabel.Text = "";
		deleteLabel.Size = new Size(1000, 20);
		deleteLabel.Font = slightlySmallerFont;


		Controls.Add(playButton);
		Controls.Add(pauseButton);
		Controls.Add(skipButton);
		Controls.Add(deleteButton);
		Controls.Add(deleteLabel);
		Controls.Add(volumeSlider);
		Controls.Add(statusLabel);
		Controls.Add(shuffleCheckBox);

		//Set up the music player
		musicPlayer = new MusicPlayer();
		musicPlayer.setOnPlaybackStopped(onPlaybackStopped);
		setVolumeBasedOnSliderValue();
		//loadPlaylistByIdx(0);
		//playNextSong();

		updateStatusLabel();

		//Hotkeys
		playPauseHandler = new KeyHandler(Keys.F8, this);
		playPauseHandler.Register();

		skipHandler = new KeyHandler(Keys.F9, this);
		skipHandler.Register();

		volumeUpHandler = new KeyHandler(Keys.F11, this);
		//volumeUpHandler.Register();

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

	long lastSkipMilliseconds = -1;
	private void HandleSkipHotkey() {
		long currentMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		if (currentMillis - lastSkipMilliseconds >= skipButtonInterval) {
			playNextSong();
			lastSkipMilliseconds = currentMillis;
		}
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

	//Playlist functions

	//Populate a combo box from a given table, assumes the table is a category
	void populateComboBox(LuaTable table, ComboBox comboBox) {
		for (int q = 1; q <= getTableSize(table); q++) {
			LuaTable innerTable = table[q] as LuaTable;
			comboBox.Items.Add(innerTable["name"]);
		}
	}

	//This function hard assumes that the combo boxes lead to a playlist
	void loadPlaylistFromComboBoxes(bool replaying = false) {
		List<string> orderedList = new List<string>();
		if ((string)playlistComboBoxes[0].SelectedItem == "From files") {
			if (!replaying) {
				OpenFileDialog fileDialog = new OpenFileDialog();
				fileDialog.CheckFileExists = true;
				fileDialog.Multiselect = true;
				fileDialog.ShowDialog();

				fileDialogSelection = new List<string>();
				orderedList.AddRange(fileDialog.FileNames);
				fileDialogSelection.AddRange(fileDialog.FileNames);
			} else {
				orderedList = new List<string>(fileDialogSelection);
			}

			playlist = new string[orderedList.Count];
		} else {
			LuaFunction playlistFunc = getCurrentlySelectedPlaylistTable()["playlistFunc"] as LuaFunction;
			orderedList.AddRange(playlistFunc.Call()[0] as string[]);
			Console.WriteLine(orderedList.Count);
			playlist = new string[orderedList.Count];
		}
		Console.WriteLine("playlist Length is " + playlist.Length);
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

	//Utility functions

	LuaFunction tableSizeFunction = null;
	int getTableSize(LuaTable table) {
		//On first run init the table size function
		if (tableSizeFunction == null) {
			tableSizeFunction = luaState.DoString("return function(tab) return #tab end")[0] as LuaFunction;
		}
		return (int)(Int64)tableSizeFunction.Call(table)[0];
	}

	private void updateStatusLabel() {
		if (musicPlayer.isPlaying()) {
			statusLabel.Text = "Now Playing: " + Path.GetFileNameWithoutExtension(currentSongFilename);
		}
		else {
			statusLabel.Text = "Now Playing: Paused";
		}
	}
	private void playNextSong() {
		Console.WriteLine("Call to playNextSong");
		playlistCounter++;
		if (playlistCounter > playlist.Length - 1) {
			Console.WriteLine("playlistCount is grater than length-1");
			loadPlaylistFromComboBoxes(true);
			playlistCounter++;
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

	string fileToDelete;
	bool waitingForPermission = false;
	CancellationTokenSource cancellationToken = new CancellationTokenSource();
	private void onDeleteButtonClick(object sender, EventArgs eventArgs) {
		//Move focus off of this control
		playButton.Focus();
		//If we haven't clicked yet
		if (!waitingForPermission) {
			waitingForPermission = true;
			fileToDelete = currentSongFilename;
			deleteLabel.Text = "Are you sure?";

			//stopwatch.Restart();
			cancellationToken.Cancel();
			cancellationToken = new CancellationTokenSource();
			Task.Delay(deleteButtonDelayMillis).ContinueWith(t => resetDeleteButtonState(cancellationToken));

		}
		else { //If we already clicked once
			   //Make sure the song hasn't changed
			if (fileToDelete == currentSongFilename) {
				//Delete the file
				if (File.Exists(fileToDelete))
					File.Delete(fileToDelete);

				//Just make sure the reset function doesn't trigger at a weird time
				cancellationToken.Cancel();
				//And then trigger it
				resetDeleteButtonState(null);
				//And then skip the now deleted song
				playNextSong();
			}
		}
	}

	private void resetDeleteButtonState(CancellationTokenSource token) {
		if (token != null && token.IsCancellationRequested) return;
		waitingForPermission = false;
		deleteLabel.Text = "";
		fileToDelete = "";
	}

	//When the song ends
	private void onPlaybackStopped(object sender, EventArgs eventArgs) {
		if (!weStoppedTheSong)
			playNextSong();
		weStoppedTheSong = false;
	}
	//ComboBox events
	private void playlistComboxBoxSelectionId0(object sender, EventArgs eventArgs) {
		playlistComboBoxSelectionChanged(sender as ComboBox, 0);
	}
	private void playlistComboxBoxSelectionId1(object sender, EventArgs eventArgs) {
		playlistComboBoxSelectionChanged(sender as ComboBox, 1);
	}
	private void playlistComboxBoxSelectionId2(object sender, EventArgs eventArgs) {
		playlistComboBoxSelectionChanged(sender as ComboBox, 2);
	}
	private void playlistComboBoxSelectionChanged(ComboBox comboBox, int idx) {
		//Clear the proceeding combo boxes
		for (int q = idx + 1; q < playlistComboBoxes.Length; q++) {
			playlistComboBoxes[q].Items.Clear();
			playlistComboBoxes[q].SelectedIndex = -1;
			playlistComboBoxes[q].Text = "";
		}
		//If it's from files then don't bother doing anything with Lua
		if ((string)playlistComboBoxes[0].SelectedItem == "From files") {
			loadPlaylistFromComboBoxes();
			playNextSong();
			return;
		}
		LuaTable baseTable = getCurrentlySelectedPlaylistTable();
		//If this isn't the last one, regen the entries of the next one
		if (idx < playlistComboBoxes.Length - 1 && (bool)baseTable["isCategory"]) {
			populateComboBox(baseTable, playlistComboBoxes[idx + 1]);
		} //If this is a playlist
		else if (!(bool)baseTable["isCategory"]) {
			loadPlaylistFromComboBoxes();


/*
 * So where I'm at right now is that the from files thing has a bunch of repeated code, gonna try to cut down on that
 * and in doing so combine the two things, such that it won't have weird bugs
 * 
 */
			playNextSong();
		}
	}

	private LuaTable getCurrentlySelectedPlaylistTable() {
		LuaTable retTable = playlistTable;
		for (int q = 0; q < playlistComboBoxes.Length; q++) {
			ComboBox comboBox = playlistComboBoxes[q];
			if (comboBox.SelectedIndex >= 0) {
				retTable = retTable[comboBox.SelectedIndex + 1] as LuaTable;
			}
			else {
				break;
			}
		}
		return retTable;
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

#if DEBUG
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	static extern bool AllocConsole();
#endif
}
