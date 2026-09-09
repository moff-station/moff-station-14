namespace Content.Client._Moffstation.Voting.Ui;

/// <summary>
/// Interfact for a en entry into the ESVotingWindow. Meant to reduce boilerplate and hardcoding into the main ESVoteControl Class
/// </summary>
public interface IVoteEntryControl
{
    void Update(EntityUid owner);
}

