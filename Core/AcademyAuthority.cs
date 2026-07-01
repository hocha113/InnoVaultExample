using InnoVault;

namespace InnoVaultExample.Core
{
    /// <summary>
    /// Shared authority gate for SyncVar, DataModule, SaveWorld, and TP mutations.
    /// </summary>
    internal static class AcademyAuthority
    {
        /// <summary>
        /// True when this peer may mutate synced or persisted academy state.
        /// </summary>
        internal static bool HasAuthority()
            // Trap · isServer alone is false in SinglePlayer; local world still needs these writes.
            => VaultUtils.isServer || VaultUtils.isSinglePlayer;
    }
}
