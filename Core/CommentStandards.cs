namespace InnoVaultExample.Core
{
    /// <summary>
    /// Shared trap comment prefixes for grep and Catalog UI trap gallery.
    /// </summary>
    internal static class CommentStandards
    {
        internal const string TrapDedServ =
            "Trap · dedServ: skip all PRT spawn when Main.dedServ is true (includes listen-host client).";

        internal const string TrapListenHost =
            "Trap · listen-host: isServer is true on listen-host player; false on host client for draw-only paths.";

        internal const string TrapSyncVarAuthority =
            "Trap · SyncVar authority: mutate only when AcademyAuthority.HasAuthority() (server or single player).";

        internal const string TrapActorSpawn =
            "Trap · Actor spawn: client calls ActorLoader.NewActor; framework handles MP routing.";

        internal const string TrapSaveMod =
            "Trap · SaveMod: DoLoad/DoSave are manual; IVaultLoader.SetupData is the production hook.";
    }
}
