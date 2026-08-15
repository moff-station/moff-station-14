using Content.Client._Starlight.UserInterface.Controls;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.Message;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Shared._Moffstation.Objectives;
using Content.Shared.DetailExaminable;
using Content.Shared.Humanoid;
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
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Moffstation.CharacterMenu;

[UsedImplicitly]
public sealed partial class CharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;


    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;

    private const int DescriptionWordLimit = 40;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MindRoleTypeChangedEvent>(OnRoleTypeChanged);
    }

    private MoffCharacterWindow? _window;
    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<UserInterface.Systems.MenuBar.Widgets.GameTopMenuBar>()?.CharacterButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<MoffCharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);
        LayoutContainer.SetGrowHorizontal(_window, LayoutContainer.GrowDirection.Both);
        LayoutContainer.SetGrowVertical(_window, LayoutContainer.GrowDirection.Both);

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

    private void CharacterUpdated(CharacterInfoSystem.CharacterData data)
    {
        if (_window == null)
        {
            return;
        }

        var (entity, objectives, minds, briefing, jobId, entityName) = data; // Starlight - Collective Mind - Added minds variable.

        _window.SpriteView.SetEntity(entity);

        UpdateRoleType();
        var job = _prototypeManager.Index(jobId);
        _window.NameLabel.SetMarkup(Loc.GetString("character-info-name-format",
            ("name", FormattedMessage.EscapeText(entityName))));
        if (job != null)
        {
            _window.SubText.SetMarkup(Loc.GetString("character-info-job-format", ("job", Loc.GetString(job.Name))));
            var jobIcon = _prototypeManager.Index(job!.Icon);
            _window.JobIcon.Texture = _sprite.Frame0(jobIcon.Icon);
        }

        _window.CharacterInfo.Visible = _ent.TryGetComponent<HumanoidProfileComponent>(entity, out var profile);
        if (profile != null)
        {
            _window.CharacterInfo.Text = Loc.GetString("character-info-details-format",
                ("gender", profile.Gender),
                ("age", profile.Age),
                ("species", Loc.GetString(_prototypeManager.Index(profile.Species).Name)));
        }

        _window.DetailedDescription.Visible = _ent.TryGetComponent<DetailExaminableComponent>(entity, out var description);
        if (description != null)
        {
            _window.DetailedDescription.SetMarkupPermissive(Loc.GetString("character-info-description-format",
                ("description", TruncateWords(description.Content, DescriptionWordLimit))));
        }

        _window.Objectives.RemoveAllChildren();
        _window.Briefing.RemoveAllChildren();
        _window.Minds.RemoveAllChildren(); // Starlight - Collective Mind

        var canPickObjectives = _ent.TryGetComponent<MindContainerComponent>(_player.LocalEntity, out var mindContainer)
            && mindContainer.Mind is not null
            && _ent.HasComponent<PotentialObjectivesComponent>(mindContainer.Mind);
        _window.AddObjectiveButtons(objectives.Count, canPickObjectives);

        foreach (var (_, conditions) in objectives)
        {
            foreach (var condition in conditions)
            {
                _window.Objectives.AddChild(new ObjectiveConditionsControl(condition, _sprite));
            }
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
            _window.Briefing.AddChild(briefingControl);
        }

        var controls = _characterInfo.GetCharacterInfoControls(entity);
        foreach (var control in controls)
        {
            _window.Objectives.AddChild(control);
        }

        _window.RolePlaceholder.Visible = false;

        _window.Objectives.InvalidateMeasure();
        _window.ObjectivesWrapper.InvalidateMeasure();
        _window.ObjectivesScroll.InvalidateMeasure();
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

        if (mind.Subtype.HasValue)
        {
            SetRoleType(Loc.GetString(mind.Subtype.Value), mind.SubtypeColor ?? proto?.Color ?? Color.White);
            return;
        }

        SetRoleType(Loc.GetString(proto?.Name ?? "role-type-crew-aligned-name"), proto?.Color ?? Color.White);
    }

    private void SetRoleType(string role, Color color)
    {
        _window!.RoleType.Text = Loc.GetString("character-info-role-type-format",
            ("color", color.ToHex()),
            ("role", role));
    }

    private static string TruncateWords(string text, int wordLimit)
    {
        var words = text.Split((char[]?) null, wordLimit + 1, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= wordLimit)
            return text;

        return Loc.GetString("character-info-description-truncated",
            ("description", string.Join(' ', words[..wordLimit])));
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

    public void OpenWindow()
    {
        if (_window == null)
            return;

        _characterInfo.RequestCharacterInfo();

        if (_window.IsOpen)
            return;

        CharacterButton?.SetClickPressed(true);
        _window.Open();
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
