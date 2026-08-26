using LibGit2Sharp;

namespace PassWinmenu.ExternalPrograms
{
	public interface ISyncService
	{
		void AddPassword(string passwordFilePath);
		void EditPassword(string passwordFilePath);
		void RenamePassword(string oldPasswordFilePath, string newPasswordFilePath);
		void DeletePassword(string passwordFilePath);
		RepositoryStatus Commit();
		void Fetch();
		void Push();
		void Rebase();
		TrackingDetails GetTrackingDetails();
	}
}
