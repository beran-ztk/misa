# Publishing Resona

The application version is maintained in `src/Resona/Resona.csproj`. A public release
is created only when a matching `v` tag is pushed.

For example, to publish version `0.1.0`:

```powershell
git add src/Resona/Resona.csproj
git commit -m "Prepare release 0.1.0"
git tag v0.1.0
git push misa HEAD
git push misa v0.1.0
```

The release workflow rejects a tag if `v0.1.0` does not match
`<Version>0.1.0</Version>` in the project. GitHub Actions then publishes the
self-contained Windows x64 application, packages it with Velopack, and uploads
the installer and update feed to the matching GitHub release.

Do not change the Velopack package ID `Beran.Music` after the first release.
Changing it would create a separate installation instead of updating the
existing one.
