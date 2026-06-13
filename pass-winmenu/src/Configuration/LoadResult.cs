namespace PassWinmenu.Configuration
{
	public abstract record LoadResult
	{
		internal record Success(ConfigManager ConfigManager) : LoadResult;
		internal record NeedsUpgrade : LoadResult;
		internal record NotFound : LoadResult;
		internal record NewFileCreated : LoadResult;
	}
}
