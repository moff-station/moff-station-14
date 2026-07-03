using System.Linq;
using Content.Client._Starlight.UserInterface.Controls; // Starlight - Collective Mind
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client._Moffstation.CharacterMenu; // Moffstation - Character Menu Redesign
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Shared._Moffstation.Objectives; // Moffstation
using Content.Shared.Input;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Client.CharacterInfo.CharacterInfoSystem;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Character;

[UsedImplicitly]
public sealed partial class CharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;


    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MindRoleTypeChangedEvent>(OnRoleTypeChanged);
    }

    // Moffstation - Start - Character Menu Redesign
    // private CharacterWindow? _window;
    private MoffCharacterWindow? _window;
    // Moffstation - End
    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.CharacterButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        // Moffstation - Start - Character Menu Redesign
        // _window = UIManager.CreateWindow<CharacterWindow>();
        _window = UIManager.CreateWindow<MoffCharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);
        LayoutContainer.SetGrowHorizontal(_window, LayoutContainer.GrowDirection.Both);
        LayoutContainer.SetGrowVertical(_window, LayoutContainer.GrowDirection.Both);
        // Moffstation - End

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CharacterUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        CommandBinds.Unregister<CharacterUIController>();
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
        _player.LocalPlayerDetached += CharacterDetached;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
        _player.LocalPlayerDetached -= CharacterDetached;
    }

    public void UnloadButton()
    {
        if (CharacterButton == null)
        {
            return;
        }

        CharacterButton.OnPressed -= CharacterButtonPressed;
    }

    public void LoadButton()
    {
        if (CharacterButton == null)
        {
            return;
        }

        CharacterButton.OnPressed += CharacterButtonPressed;
    }

    private void DeactivateButton()
    {
        if (CharacterButton == null)
        {
            return;
        }

        CharacterButton.Pressed = false;
    }

    private void ActivateButton()
    {
        if (CharacterButton == null)
        {
            return;
        }

        CharacterButton.Pressed = true;
    }

    private void CharacterUpdated(CharacterData data)
    {
        if (_window == null)
        {
            return;
        }

        var (entity, job, objectives, minds, briefing, entityName) = data; // Starlight - Collective Mind - Added minds variable.

        _window.SpriteView.SetEntity(entity);

        UpdateRoleType();

        _window.NameLabel.Text = entityName;
        _window.SubText.Text = job;
        _window.Objectives.RemoveAllChildren();
        // Moffstation - Start - Character Menu Redesign (removed ObjectivesLabel, added Briefing clear)
        // _window.ObjectivesLabel.Visible = objectives.Any();
        _window.Briefing.RemoveAllChildren();
        // Moffstation - End
        _window.Minds.RemoveAllChildren(); // Starlight - Collective Mind

        // Moffstation - Start - Character Menu Redesign (moved button logic to MoffCharacterWindow)
        var canPickObjectives = _ent.TryGetComponent<MindContainerComponent>(_player.LocalEntity, out var mindContainer)
            && mindContainer.Mind is not null
            && _ent.HasComponent<PotentialObjectivesComponent>(mindContainer.Mind);
        _window.AddObjectiveButtons(objectives.Count, canPickObjectives);
        // Moffstation - End

        foreach (var (groupId, conditions) in objectives)
        {

            // Moff Start - New Character UI
            //var objectiveControl = new CharacterObjectiveControl
            //{
            //   Orientation = BoxContainer.LayoutOrientation.Vertical,
            //    Modulate = Color.Gray
            //};
            //
            // var objectiveText = new FormattedMessage();
            // objectiveText.TryAddMarkup(groupId, out _);
            //
            // var objectiveLabel = new RichTextLabel
            // {
            //     StyleClasses = { StyleClass.TooltipTitle },
            //     HorizontalAlignment = Control.HAlignment.Center
            // };
            // objectiveLabel.SetMessage(objectiveText);
            // _window.Objectives.AddChild(objectiveLabel);

            foreach (var condition in conditions)
            {
                _window.Objectives.AddChild(new ObjectiveConditionsControl(condition, _sprite));
            }
            // Moffstation - End
        }

        // Starlight - Start - Collective Mind
        if (minds != null && minds.Count > 0)
        {
            var mindsControl = new CharacterMindsControl
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
            };
            var mindDescriptionMessage = new FormattedMessage();
            mindDescriptionMessage.AddText("Available collective minds:");
            foreach (var mindPrototype in minds)
            {
                if (!_prototypeManager.Resolve(mindPrototype.Key, out var mindProto))
                    continue;

                mindDescriptionMessage.AddText("\n");
                mindDescriptionMessage.PushColor(mindProto.Color);
                mindDescriptionMessage.AddText($"{mindProto.LocalizedName}: +{mindProto.KeyCode}");
                mindDescriptionMessage.AddText($" (Number {mindPrototype.Value.MindId})");
                mindDescriptionMessage.Pop();

            }
            mindsControl.Description.SetMessage(mindDescriptionMessage);
            _window.Minds.AddChild(mindsControl); // Moffstation - Character Menu Redesign (fix: Minds was declared but never populated)
        }
        // Starlight - End

        if (briefing != null)
        {
            var briefingControl = new ObjectiveBriefingControl();
            var text = new FormattedMessage();
            text.PushColor(Color.Yellow);
            text.AddText(briefing);
            briefingControl.Label.SetMessage(text);
            _window.Briefing.AddChild(briefingControl); // Moffstation - Character Menu Redesign
        }

        var controls = _characterInfo.GetCharacterInfoControls(entity);
        foreach (var control in controls)
        {
            _window.Objectives.AddChild(control);
        }

        // Moffstation - Start - hide "no special roles" placeholder
        // _window.RolePlaceholder.Visible = briefing == null && !controls.Any() && !objectives.Any();
        _window.RolePlaceholder.Visible = false;
        // Moffstation - End
    }

    private void OnRoleTypeChanged(MindRoleTypeChangedEvent ev, EntitySessionEventArgs _)
    {
        UpdateRoleType();
    }

    private void UpdateRoleType()
    {
        if (_window == null || !_window.IsOpen)
            return;

        if (!_ent.TryGetComponent<MindContainerComponent>(_player.LocalEntity, out var container)
            || container.Mind is null)
            return;

        if (!_ent.TryGetComponent<MindComponent>(container.Mind.Value, out var mind))
            return;

        if (!_prototypeManager.TryIndex(mind.RoleType, out var proto))
            Log.Error($"Player '{_player.LocalSession}' has invalid Role Type '{mind.RoleType}'. Displaying default instead");

        // Moffstation - Start - Faction subtype display
        if (mind.Subtype.HasValue)
        {
            _window.RoleType.Text = Loc.GetString(mind.Subtype.Value);
            _window.RoleType.FontColorOverride = mind.SubtypeColor ?? proto?.Color ?? Color.White;
            return;
        }
        // Moffstation - End

        _window.RoleType.Text = Loc.GetString(proto?.Name ?? "role-type-crew-aligned-name");
        _window.RoleType.FontColorOverride = proto?.Color ?? Color.White;
    }

    private void CharacterDetached(EntityUid uid)
    {
        CloseWindow();
    }

    private void CharacterButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        CharacterButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            _characterInfo.RequestCharacterInfo();
            _window.Open();
        }
    }
}
