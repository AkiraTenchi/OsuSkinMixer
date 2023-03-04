using Godot;
using System.Threading;
using System.Threading.Tasks;
using System;
using OsuSkinMixer.Components;
using OsuSkinMixer.Models;

namespace OsuSkinMixer.StackScenes;

public partial class SkinMixer : StackScene
{
	public override string Title => "Skin Mixer";

	private CancellationTokenSource CancellationTokenSource;

	private PackedScene SkinInfoScene;

	private LoadingPopup LoadingPopup;
	private SkinNamePopup SkinNamePopup;
	private SkinOptionsSelector SkinOptionsSelector;
	private Button CreateSkinButton;
	private Button RandomButton;

	public override void _Ready()
	{
		SkinInfoScene = GD.Load<PackedScene>("res://src/StackScenes/SkinInfo.tscn");

		LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");
		SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
		SkinOptionsSelector = GetNode<SkinOptionsSelector>("%SkinOptionsSelector");
		CreateSkinButton = GetNode<Button>("%CreateSkinButton");
		RandomButton = GetNode<Button>("%RandomButton");

		LoadingPopup.CancelAction = OnCancelButtonPressed;
		CreateSkinButton.Pressed += OnCreateSkinButtonPressed;
		RandomButton.Pressed += OnRandomButtonPressed;

		SkinNamePopup.ConfirmAction = s =>
		{
			SkinNamePopup.Out();
			RunSkinCreator(s);
		};

		SkinOptionsSelector.CreateOptionComponents("<<DEFAULT SKIN>>");
	}

	private void OnCreateSkinButtonPressed()
	{
		SkinNamePopup.In();
	}

	private void OnRandomButtonPressed()
	{
		SkinOptionsSelector.Randomize();

		EmitSignal(SignalName.ToastPushed, "Randomized skin options");
	}

	private void RunSkinCreator(string skinName)
	{
		LoadingPopup.In();

		SkinMixerMachine skinCreator = new()
		{
			NewSkinName = skinName,
			SkinOptions = SkinOptionsSelector.SkinOptions,
			ProgressChanged = LoadingPopup.SetProgress,
		};

		CancellationTokenSource = new CancellationTokenSource();
		Task.Run(() => skinCreator.Run(CancellationTokenSource.Token))
			.ContinueWith(t =>
			{
				LoadingPopup.Out();

				var ex = t.Exception;
				if (ex != null)
				{
					if (ex.InnerException is OperationCanceledException)
						return;

					GD.PrintErr(ex);
					OS.Alert($"{ex.Message}\nPlease report this error with logs.", "Skin creation failure");
					return;
				}

				var skinInfoInstance = SkinInfoScene.Instantiate<SkinInfo>();
				skinInfoInstance.Skins = new OsuSkin[] { skinCreator.NewSkin };
				EmitSignal(SignalName.ScenePushed, skinInfoInstance);
			});
	}

	private void OnCancelButtonPressed()
	{
		CancellationTokenSource?.Cancel();
	}
}
