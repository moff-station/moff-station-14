using System.Linq;
using System.Numerics;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Station;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Silicons.Borgs;

public abstract partial class SharedBorgSensorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] protected SharedSuitSensorSystem Sensor = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform =  default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [Dependency] private EntityQuery<BorgSensorComponent> _sensorQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BorgSensorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StationId ??= _station.GetOwningStation(ent.Owner);

        ent.Comp.NextUpdate = _timing.CurTime;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnVerb(Entity<BorgSensorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (ent.Comp.ControlsLocked || !args.CanInteract || !_interaction.InRangeUnobstructed(args.User, args.Target))
            return;

        var user = args.User;
        args.Verbs.UnionWith(Enum.GetValues<SuitSensorMode>().Select(it => CreateVerb(ent, user, it)));
    }

    private Verb CreateVerb(Entity<BorgSensorComponent> ent, EntityUid userUid, SuitSensorMode mode)
    {
        return new Verb
        {
            Text = Sensor.GetModeName(mode),
            Message = Sensor.GetModeDescription(mode),
            Disabled = ent.Comp.Mode == mode,
            Priority = -(int)mode, // sort them in descending order
            Category = VerbCategory.SetSensor,
            Act = () => TrySetSensor(ent.AsNullable(), mode, userUid)
        };
    }

    private bool TrySetSensor(Entity<BorgSensorComponent?> ent, SuitSensorMode mode, EntityUid userUid)
    {
        if (!_sensorQuery.HasComp(ent))
            return false;

        if (ent.Owner ==  userUid)
            ent.Comp.Mode = mode;
        else
        {
            var doAfterEvent = new SuitSensorChangeDoAfterEvent(mode);
            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, ent.Comp.SensorsTime, doAfterEvent, ent)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
        }
        return true;
    }

    private static void OnBorgSensorDoAfter(Entity<BorgSensorComponent> ent, ref SuitSensorChangeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        ent.Comp.Mode = args.Mode;
    }


    public SuitSensorStatus? GetSensorState(Entity<BorgSensorComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return null;

        var sensor = ent.Comp1;
        var transform = ent.Comp2;

        if (sensor.Mode == SuitSensorMode.SensorOff ||
            !_mobStateQuery.TryComp(ent, out var mobState) ||
            transform.GridUid == null)
            return null;

        var sensorType = sensor.SensorType;
        var userName = MetaData(ent.Owner).EntityName;
        var userJob = Loc.GetString("job-name-borg");
        var userJobIcon = "JobIconBorg";
        var userJobDepartments = new List<string>{Loc.GetString("department-Silicon")};

        // get health mob state
        var isAlive = _mobState.IsAlive(ent, mobState);

        // get mob total damage
        var totalDamage = _damageable.GetTotalDamage(ent.Owner).Int();

        // Get mob total damage crit threshold
        int? totalDamageThreshold = null;
        if (_threshold.TryGetThresholdForState(ent, MobState.Critical, out var critThreshold))
            totalDamageThreshold = critThreshold.Value.Int();

        // finally, form suit sensor status
        var status = new SuitSensorStatus(GetNetEntity(ent), GetNetEntity(ent.Owner), userName, userJob, userJobIcon, userJobDepartments)
        {
            SensorType = sensorType, // Moffstation - Borg sensors
        };
        switch (sensor.Mode)
        {
            case SuitSensorMode.SensorBinary:
                status.IsAlive = isAlive;
                break;
            case SuitSensorMode.SensorVitals:
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                break;
            case SuitSensorMode.SensorCords:
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                EntityCoordinates coordinates;

                if (transform.GridUid != null)
                {
                    coordinates = new EntityCoordinates(transform.GridUid.Value,
                        Vector2.Transform(_transform.GetWorldPosition(transform),
                            _transform.GetInvWorldMatrix(transform.GridUid.Value)));
                }
                else if (transform.MapUid != null)
                {
                    coordinates = new EntityCoordinates(transform.MapUid.Value,
                        _transform.GetWorldPosition(transform));
                }
                else
                {
                    coordinates = EntityCoordinates.Invalid;
                }

                status.Coordinates = GetNetCoordinates(coordinates);
                break;
        }

        return status;
    }

    public bool CheckSensorAssignedStation(Entity<BorgSensorComponent> sensor)
    {
        if (!sensor.Comp.StationId.HasValue && Transform(sensor.Owner).GridUid == null)
            return false;

        sensor.Comp.StationId = _station.GetOwningStation(sensor.Owner);
        Dirty(sensor);
        return sensor.Comp.StationId.HasValue;
    }
}
