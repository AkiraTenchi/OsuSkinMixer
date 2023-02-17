using Godot;
using System;
using System.Collections.Generic;

namespace OsuSkinMixer;

public partial class Main : Control
{
	private PackedScene MenuScene;

	private AnimationPlayer ScenesAnimationPlayer;
	private Control ScenesControl;
	private TextureButton BackButton;

	private Stack<StackScene> SceneStack { get; } = new();

	private StackScene PendingScene { get; set; }

	public override void _Ready()
	{
		MenuScene = GD.Load<PackedScene>("res://src/Menu.tscn");

		ScenesAnimationPlayer = GetNode<AnimationPlayer>("ScenesAnimationPlayer");
		ScenesControl = GetNode<Control>("Scenes");
		BackButton = GetNode<TextureButton>("TopBar/HBoxContainer/BackButton");

		ScenesAnimationPlayer.AnimationFinished += (animationName) =>
		{
			if (animationName == "pop_out")
			{
				SceneStack.Pop().QueueFree();
				SceneStack.Peek().Visible = true;

				ScenesAnimationPlayer.Play("pop_in");
			}
			else if (animationName == "push_out")
			{
				if (SceneStack.TryPeek(out StackScene currentlyActiveScene))
					currentlyActiveScene.Visible = false;

				PendingScene.ScenePushed += PushScene;
				PendingScene.Visible = true;
				SceneStack.Push(PendingScene);
				ScenesControl.AddChild(PendingScene);

				PendingScene = null;
				ScenesAnimationPlayer.Play("push_in");
			}
		};

		BackButton.Pressed += PopScene;

		PushScene(MenuScene.Instantiate<StackScene>());
	}

	private void PushScene(StackScene scene)
	{
		PendingScene = scene;
		ScenesAnimationPlayer.Play("push_out");
	}

	private void PopScene()
	{
		ScenesAnimationPlayer.Play("pop_out");
	}
}
