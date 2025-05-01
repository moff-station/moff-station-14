using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.RoundReport.Components;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class RoundReportComponent : Component
{
    [DataField("reportHeader"), AutoNetworkedField]
    public string ReportHeader = "";

    [DataField("reportBody"), AutoNetworkedField]
    public string ReportBody = "";

    [DataField("headerColor"), AutoNetworkedField]
    public string HeaderColor = "white";

    [DataField("bodyColor"), AutoNetworkedField]
    public string BodyColor = "grey";
}
