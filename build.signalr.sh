dotnet build \
	Mimer.Notes.SignalR/Mimer.Notes.SignalR.csproj \
	-p:DeployOnBuild=true \
	-p:PublishProfile=FolderProfile1

echo "Build completed at: $(date)"
