using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Vorbis;

public class MusicPlayer {
	private WaveOutEvent outputDevice;
	private MediaFoundationReader audioFileReader;
	private VorbisWaveReader vorbisReader;
	
	public MusicPlayer() {
		outputDevice = new WaveOutEvent();
	}

	public void play(string filename) {
		outputDevice.Stop();
			if (System.IO.Path.GetExtension(filename) == ".ogg") {
			vorbisReader = new NAudio.Vorbis.VorbisWaveReader(filename);
			outputDevice.Init(vorbisReader);
		}
		else {
			audioFileReader = new MediaFoundationReader(filename);
			outputDevice.Init(audioFileReader);
		}
		outputDevice.Play();
	}
	public void resume() {
		outputDevice.Play();
	}
	public void setVolume(float volume) {
		outputDevice.Volume = volume;
	}
	public void pause() {
		outputDevice.Pause();
	}
	public void stop() {
		outputDevice.Stop();
	}
	public bool isPlaying() {
		return outputDevice.PlaybackState == PlaybackState.Playing;
	}
	public bool isPaused() {
		return outputDevice.PlaybackState == PlaybackState.Paused;
	}
	public bool isStopped() {
		return outputDevice.PlaybackState == PlaybackState.Stopped;
	}
	public void setOnPlaybackStopped(System.EventHandler<NAudio.Wave.StoppedEventArgs> handler) {
		outputDevice.PlaybackStopped += handler;
	}
}