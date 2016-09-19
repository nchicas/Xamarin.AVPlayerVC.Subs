using System;
using AVFoundation;
using Foundation;
using UIKit;
using AVKit;

namespace Xamarin.AVPlayerVC.Subs
{
	public partial class ViewController : UIViewController
	{
		protected ViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			NSUrl movieUrl = NSUrl.FromString("http://devstreaming.apple.com/videos/wwdc/2016/504m956dgg4hlw2uez9/504/504_sd_whats_new_in_http_live_streaming.mp4?dl=1");
			AVUrlAsset asset = AVUrlAsset.Create(movieUrl);
			AVPlayerItem item = AVPlayerItem.FromAsset(asset);

			AVPlayer player = AVPlayer.FromPlayerItem(item);
			AVPlayerViewController controller = new AVPlayerViewController();
			controller.Player = player;

			AddChildViewController(controller);
			View.AddSubview(controller.View);
			controller.View.Frame = View.Frame;

			controller.ShowSubtitles(
				NSUrl.FromString("https://raw.githubusercontent.com/eplt/WWDC-Video-Subtitles/master/2016/SD/English/504_sd_whats_new_in_http_live_streaming_eng.srt")
			);

			player.Play();
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}
