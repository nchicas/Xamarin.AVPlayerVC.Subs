using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using AVKit;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using ObjCRuntime;
using UIKit;

using SubtitlesParser.Classes.Parsers;

namespace Xamarin.AVPlayerVC.Subs
{
	public static class Subtitles
	{
		#region "Extension properties"

		enum AssociationPolicy
		{
			Assign = 0,
			RetainNonAtomic = 1,
			CopyNonAtomic = 3,
			Retain = 01401,
			Copy = 01403,
		}

		[DllImport("/usr/lib/libobjc.dylib")]
		static extern void objc_setAssociatedObject(
			IntPtr pointer, IntPtr key, 
			IntPtr value, AssociationPolicy policy);

		[DllImport("/usr/lib/libobjc.dylib")]
		static extern IntPtr objc_getAssociatedObject(
			IntPtr pointer, IntPtr key);

		private static T GetProperty<T>(
			this AVPlayerViewController controller, 
			NSString propertyKey) where T : NSObject
		{
			var pointer = objc_getAssociatedObject(
				controller.Handle,
				propertyKey.Handle
			);

			return Runtime.GetNSObject<T>(pointer);
		}

		private static void SetProperty<T>(
			this AVPlayerViewController controller,
			NSString propertyKey,
			T value,
			AssociationPolicy policy) where T : NSObject
		{
			objc_setAssociatedObject(
				controller.Handle,
				propertyKey.Handle,
				value.Handle,
				policy
			);
		}

		#endregion

		struct SubtitleKeys
		{
			public static NSString Label = new NSString("LabelKey");
			public static NSString Height = new NSString("HeightKey");
			public static NSString Payload = new NSString("PayloadKey");
		}

		public static UILabel SubtitleLabel(
			this AVPlayerViewController controller)
		{
			return GetProperty<UILabel>(
				controller, 
				SubtitleKeys.Label
			);
		}

		public static void SetSubtitleLabel(
			this AVPlayerViewController controller, 
			UILabel label)
		{
			SetProperty<UILabel>(
				controller, 
				SubtitleKeys.Label, 
				label, 
				AssociationPolicy.RetainNonAtomic
			);
		}

		private static NSLayoutConstraint SubtitleConstranint(
			this AVPlayerViewController controller)
		{
			return GetProperty<NSLayoutConstraint>(
				controller, 
				SubtitleKeys.Height
			);
		}

		private static void SetSubtitleConstraint(
			this AVPlayerViewController controller, 
			NSLayoutConstraint constraint)
		{
			SetProperty<NSLayoutConstraint>(
				controller,
				SubtitleKeys.Height,
				constraint,
				AssociationPolicy.RetainNonAtomic
			);
		}

		private static NSArray Payload(
			this AVPlayerViewController controller)
		{
			return GetProperty<NSArray>(
				controller,
				SubtitleKeys.Payload
			);
		}

		private static void SetPayload(
			this AVPlayerViewController controller, 
			NSArray payload)
		{
			SetProperty<NSArray>(
				controller,
				SubtitleKeys.Payload,
				payload,
				AssociationPolicy.RetainNonAtomic
			);
		}

		public static void ShowSubtitles(
			this AVPlayerViewController controller, 
			NSUrl fileUrl)
		{			
			var subtitles = ParseSubtitles(fileUrl);
			controller.SetPayload(subtitles);
			controller.AddSubtitleLabel();
			controller.Player.AddPeriodicTimeObserver(
				new CMTime(1, 60),
				DispatchQueue.MainQueue,
				controller.ProcessSubtitle
			);
		}

		private static void AddSubtitleLabel(
			this AVPlayerViewController controller)
		{
			UILabel label = controller.SubtitleLabel();

			if (label == null ) {
				label = new UILabel();
			}

			bool isPad = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;

			label.TranslatesAutoresizingMaskIntoConstraints = false;
			label.BackgroundColor = UIColor.Clear;
			label.TextAlignment = UITextAlignment.Center;
			label.Font = UIFont.BoldSystemFontOfSize(isPad ? 40.0f : 22.0f);
			label.TextColor = UIColor.White;
			label.Lines = 0;
			label.Layer.ShadowColor = UIColor.Black.CGColor;
			label.Layer.ShadowOffset = new CGSize(1.0,1.0);
			label.Layer.ShadowOpacity = 0.9f;
			label.Layer.ShadowRadius = 1.0f;
			label.Layer.ShouldRasterize = true;
			label.Layer.RasterizationScale = UIScreen.MainScreen.Scale;
			label.LineBreakMode = UILineBreakMode.WordWrap;
			controller.SetSubtitleLabel(label);

			if (controller.ContentOverlayView != null) {
				controller.ContentOverlayView.AddSubview(label);
			}
			else {
				throw new Exception("Initialize the player view before loading subtitles");
			}

			NSLayoutConstraint[] hConstraints = NSLayoutConstraint.FromVisualFormat(
				"H:|-(20)-[l]-(20)-|", 
				0, 
				null, 
				NSDictionary.FromObjectAndKey(label, new NSString("l"))
			);
			controller.ContentOverlayView.AddConstraints(hConstraints);

			NSLayoutConstraint[] vConstraints = NSLayoutConstraint.FromVisualFormat(
				"V:[l]-(30)-|",
				0,
				null,
				NSDictionary.FromObjectAndKey(label, new NSString("l"))
			);
			controller.ContentOverlayView.AddConstraints(vConstraints);

			NSLayoutConstraint heightConstraint = NSLayoutConstraint.Create(
				label, 
				NSLayoutAttribute.Height, 
				NSLayoutRelation.Equal, 
				null, 
				NSLayoutAttribute.NoAttribute, 
				1.0f, 
				30.0f
			);
			controller.ContentOverlayView.AddConstraint(heightConstraint);
			controller.SetSubtitleConstraint(heightConstraint);
		}

		private static NSArray ParseSubtitles(NSUrl url)
		{
			var subtitles = new NSMutableArray();

			try {
				NSData data = NSData.FromUrl(url);
				var parser = new SubParser();
				byte[] bytes = data.ToArray();
				using (MemoryStream stream = new MemoryStream(bytes)) {
					foreach ( var item in parser.ParseStream(stream)) {
						var subtitle = new NSMutableDictionary();
						subtitle.Add( 
							new NSString("from"),
							NSNumber.FromFloat(item.StartTime/1000.0f)
						);
						subtitle.Add(
							new NSString("to"),
							NSNumber.FromFloat(item.EndTime/1000.0f)
						);

						string content = string.Empty;
						item.Lines.ForEach(l => content += (l + "\n"));
						content = content.Trim('\n');
						subtitle.Add(
							new NSString("text"),
							new NSString(content)
						);
						
						subtitles.Add(
							subtitle
						);
					}
				}

				return subtitles;
			}
			catch (Exception e) {
				Console.WriteLine("Error parsing subtitles: " + e.Message);
				return new NSArray();
			}
		}

		private static void ProcessSubtitle(
			this AVPlayerViewController controller,
			CMTime time)
		{
			NSString text = NSString.Empty;

			NSArray payload = controller.Payload();
			for (nuint i = 0; i < payload.Count; ++i ) {
				NSDictionary item = payload.GetItem<NSDictionary>(i);
				NSNumber from = item.ObjectForKey(new NSString("from")) as NSNumber;
				NSNumber to = item.ObjectForKey(new NSString("to")) as NSNumber;
				if ( time.Seconds >= from.FloatValue &&
				    time.Seconds < to.FloatValue) {
					text = item.ObjectForKey(new NSString("text")) as NSString;
					break;
				}
			}

			UILabel subtitleLabel = controller.SubtitleLabel();
			subtitleLabel.Text = text;
			var attributes = new UIStringAttributes();
			attributes.Font = subtitleLabel.Font;
			CGRect rect = text.GetBoundingRect(
				new CGSize(subtitleLabel.Bounds.Width, nfloat.MaxValue),
				NSStringDrawingOptions.UsesLineFragmentOrigin,
				attributes, null
			);
			controller.SubtitleConstranint().Constant = rect.Size.Height + 5;
		}
	}
}
