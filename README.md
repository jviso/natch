
# NATCH -- dotNet bATCH testing

## Developing

- [Install .NET Core SDK v3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

### TODO

- Migrate config from JSON to TOML or YAML to support comments
- Support audio formats other than MP3
- Elegantly handle non-audio files
- Traverse input directory to select audio files in child directories
- Clean up demo output to be more impactful
- Parse Impeller response for transcript
- Support constant pool for given amount of time
- Add logging

## About the appsettings.json

- Descriptions of each parameter:
  - `baseUrl`: the URL to which Natch will send audio for transcription
  - `auth`: user:pass
  - `inputDir`: a directory containing MP3 files for transcription
  - `outputDir`: a directory for writing transcription responses
  - `parseForTranscript`: if true, Natch assumes that the transcription response is from Stem for a single channel and attempts to select the transcript field
  - `workers`: how many worker processes are spawned
  - `demoMode`: if true, a splash graphic and stats are displayed

## Building a Docker image

- From the project root: `docker build -t deepgram/natch:latest -f Dockerfile .`

## Running a container

- Sample command for sending audio to localhost:
`docker run -v /path/to/appsettings.json:/App/appsettings.json:ro -v /path/to/audio_dir:/App/audio:ro -v /path/to/transcription/location:/App/output:rw --network="host" deepgram/natch:test`
