namespace K4System
{
	using CounterStrikeSharp.API;
	using CounterStrikeSharp.API.Core;
	using CounterStrikeSharp.API.Core.Plugin;

	using Microsoft.Extensions.Logging;

	public partial class ModuleStat : IModuleStat
	{
		public ModuleStat(ILogger<ModuleStat> logger, PluginContext pluginContext)
		{
			this.Logger = logger;
			this.PluginContext = pluginContext;
		}

		public void Initialize(bool hotReload)
		{
			this.Logger.LogInformation("Initializing '{0}'", this.GetType().Name);

			//** ? Forwarded Variables */

			Plugin plugin = (this.PluginContext.Plugin as Plugin)!;

			this.Config = plugin.Config;
			this.Database = plugin.Database;

			//** ? Initialize Database */

			this.Database.ExecuteNonQueryAsync($@"CREATE TABLE IF NOT EXISTS `{this.Config.DatabaseSettings.TablePrefix}k4stats` (
				`id` INT AUTO_INCREMENT PRIMARY KEY,
				`steam_id` VARCHAR(32) UNIQUE NOT NULL,
				`name` VARCHAR(255) NOT NULL,
				`lastseen` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
				`kills` INT NOT NULL DEFAULT 0,
				`deaths` INT NOT NULL DEFAULT 0,
				`assists` INT NOT NULL DEFAULT 0,
				`hits_taken` INT NOT NULL DEFAULT 0,
				`hits_given` INT NOT NULL DEFAULT 0,
				`headshots` INT NOT NULL DEFAULT 0,
				`grenades` INT NOT NULL DEFAULT 0,
				`mvp` INT NOT NULL DEFAULT 0,
				`round_win` INT NOT NULL DEFAULT 0,
				`round_lose` INT NOT NULL DEFAULT 0,
				`kda` DECIMAL(5, 2) NOT NULL DEFAULT 0,
				UNIQUE (`steam_id`)
			);");

			//** ? Register Module Parts */

			Initialize_Events(plugin);
			Initialize_Commands(plugin);

			//** ? Hot Reload Events */

			if (hotReload)
			{
				List<CCSPlayerController> players = Utilities.GetPlayers();

				var loadTasks = players
					.Where(player => player != null && player.IsValid && player.PlayerPawn.IsValid && !player.IsBot && !player.IsHLTV)
					.Select(LoadStatData)
					.ToList();

				_ = Task.WhenAll(loadTasks);
			}
		}

		public void Release(bool hotReload)
		{
			this.Logger.LogInformation("Releasing '{0}'", this.GetType().Name);

			//** ? Save Player Caches */

			_ = SaveAllPlayerCache(true);
		}
	}
}