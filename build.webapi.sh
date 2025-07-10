dotnet build \
	Mimer.Notes.WebApi/Mimer.Notes.WebApi.csproj \
	-p:DeployOnBuild=true \
	-p:PublishProfile=FolderProfile1

echo "Build completed at: $(date)"