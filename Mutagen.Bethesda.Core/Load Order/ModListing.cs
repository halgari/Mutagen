using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda
{
    /// <summary>
    /// Class associating a ModKey with a Mod object that may or may not exist.
    /// </summary>
    public class ModListing<TMod>
        where TMod : class, IModGetter
    {
        /// <summary>
        /// Mod object
        /// </summary>
        public TMod? Mod { get; private set; }
        /// <summary>
        /// ModKey associated with listing
        /// </summary>
        public ModKey Key { get; private set; }

        private ModListing(ModKey key, TMod? mod)
        {
            this.Key = key;
            this.Mod = mod;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ModListing(TMod mod)
        {
            this.Key = mod.ModKey;
            this.Mod = mod;
        }

        /// <summary>
        /// Factory to create a ModListing which does not have a mod object
        /// </summary>
        /// <param name="key">ModKey to associate with listing</param>
        /// <returns>ModListing with no mod object</returns>
        public static ModListing<TMod> UnloadedModListing(ModKey key)
        {
            return new ModListing<TMod>(key, default);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Key.ToString();
        }
    }
}
