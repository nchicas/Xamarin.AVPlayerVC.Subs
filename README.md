# Xamarin.AVPlayerVC.Subs

Xamarin.iOS Category for supporting subtitles in AVPlayerViewController.

On track for improvement:

 * Async support
 * Styling support with DTCoreText
 * A more efficient way of rendering 

Usage: Call ShowSubtitles with a remote or local subtitle file:

```csharp
...
AddChildViewController(controller);
View.AddSubview(controller.View);
controller.View.Frame = View.Frame;

controller.ShowSubtitles(NSUrl.FromString("subtitle url"));

player.Play();
```

I figure out how to create category properties in this project, so this snippet can be useful for reusing in other Xamarin.iOS project that require category properties:

```csharp
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
```

### Stuff used to make this:

 * Based on: [AVPlayerViewController-Subtitles](https://github.com/mhergon/AVPlayerViewController-Subtitles)
 * Subtitle parsing: [SubtitlesParser](https://github.com/AlexPoint/SubtitlesParser) 

