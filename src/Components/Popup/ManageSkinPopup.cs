using Godot;
using OsuSkinMixer.Models;
using OsuSkinMixer.Statics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OsuSkinMixer.Components;

public partial class ManageSkinPopup : Popup
{
	private const string HIDE_BUTTON_TEXT = "Hide from osu!";

	private const string UNHIDE_BUTTON_TEXT = "Unhide from osu!";

	private QuestionPopup DeleteQuestionPopup;
	private SkinNamePopup SkinNamePopup;
	private LoadingPopup LoadingPopup;
	private Label TitleLabel;
	private Button ModifyButton;
	private Button HideButton;
	private Button DuplicateButton;
	private Button DeleteButton;

	private OsuSkin[] _skins;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		DeleteQuestionPopup = GetNode<QuestionPopup>("%DeleteQuestionPopup");
		SkinNamePopup = GetNode<SkinNamePopup>("%SkinNamePopup");
		LoadingPopup = GetNode<LoadingPopup>("%LoadingPopup");
		TitleLabel = GetNode<Label>("%Title");
		ModifyButton = GetNode<Button>("%ModifyButton");
		HideButton = GetNode<Button>("%HideButton");
		DuplicateButton = GetNode<Button>("%DuplicateButton");
		DeleteButton = GetNode<Button>("%DeleteButton");

		DeleteQuestionPopup.ConfirmAction = OnDeleteConfirmed;
		SkinNamePopup.ConfirmAction = OnDuplicateSkinNameConfirmed;
		ModifyButton.Pressed += OnModifyButtonPressed;
		HideButton.Pressed += OnHideButtonPressed;
		DuplicateButton.Pressed += OnDuplicateButtonPressed;
		DeleteButton.Pressed += OnDeleteButtonPressed;
	}

	public void SetSkin(OsuSkin skin)
	{
		_skins = new OsuSkin[] { skin };
		TitleLabel.Text = skin.Name;
		HideButton.Text = skin.Hidden ? UNHIDE_BUTTON_TEXT : HIDE_BUTTON_TEXT;
	}

	public void SetSkins(IEnumerable<OsuSkin> skins)
	{
		_skins = skins.ToArray();
		TitleLabel.Text = $"{_skins.Length} skins selected.";
	}

	private void OnModifyButtonPressed()
	{
		OsuData.RequestSkinModify(_skins);
		Out();
	}

	private void OnHideButtonPressed()
	{
		try
		{
			OsuData.ToggleSkinsHiddenState(_skins);
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			OS.Alert("Failed to hide/unhide skin. Please report this issue with logs.", "Error");
		}

		Out();
	}

	private void OnDuplicateButtonPressed()
	{
		if (_skins.Length > 1)
		{
			SkinNamePopup.LineEditText = " (copy)";
			SkinNamePopup.SkinNames = _skins.Select(s => s.Name).ToArray();
			SkinNamePopup.SuffixMode = true;
		}
		else
		{
			SkinNamePopup.LineEditText = $"{_skins[0].Name} (copy)";
			SkinNamePopup.SkinNames = new string[] { _skins[0].Name };
			SkinNamePopup.SuffixMode = false;
		}

		SkinNamePopup.In();
	}

	private void OnDuplicateSkinNameConfirmed(string value)
	{
		OsuData.SweepPaused = true;
		LoadingPopup.In();

		Task.Run(() =>
		{
			foreach (OsuSkin skin in _skins)
				DuplicateSingleSkin(skin, SkinNamePopup.SuffixMode ? skin.Name + value : value);
		})
		.ContinueWith(t =>
		{
			OsuData.SweepPaused = false;
			LoadingPopup.Out();
			SkinNamePopup.Out();
			Out();

			if (t.IsFaulted)
			{
				GD.PrintErr(t.Exception);
				OS.Alert("Failed to duplicate skins. Please report this issue with logs.", "Error");
			}
			else
			{
				OsuData.RequestSkinInfo(_skins);
			}
		});
	}

	private static void DuplicateSingleSkin(OsuSkin skin, string newSkinName)
	{
		Settings.Log($"Duplicating skin: {skin.Name} -> {newSkinName}");
		OsuSkin newSkin = new(skin.Directory.CopyDirectory(Path.Combine(Settings.SkinsFolderPath, newSkinName), true));
		OsuData.AddSkin(newSkin);
	}

	private void OnDeleteButtonPressed()
	{
		DeleteQuestionPopup.In();
	}

	private void OnDeleteConfirmed()
	{
		OsuData.SweepPaused = true;
		LoadingPopup.In();

		Task.Run(() =>
		{
			foreach (OsuSkin skin in _skins)
			{
				Settings.Log($"Deleting skin: {skin.Name}");
				skin.Directory.Delete(true);
				OsuData.RemoveSkin(skin);
			}
		})
		.ContinueWith(t =>
		{
			OsuData.SweepPaused = false;
			LoadingPopup.Out();
			Out();

			if (t.IsFaulted)
			{
				GD.PrintErr(t.Exception);
				OS.Alert("Failed to delete skins. Please report this issue with logs.", "Error");
			}
		});
	}
}
