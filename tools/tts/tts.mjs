// Neural text-to-speech via Microsoft Edge Read Aloud (free).
// Usage: node tts.mjs <textFile> <outFileBase> [voice]
// Writes MP3 audio and prints the produced file path to stdout.
import { readFileSync, mkdirSync } from "node:fs";
import { MsEdgeTTS, OUTPUT_FORMAT } from "msedge-tts";

const [textFile, outDir, voice = "en-US-JennyNeural"] = process.argv.slice(2);
if (!textFile || !outDir) {
    console.error("usage: node tts.mjs <textFile> <outDir> [voice]");
    process.exit(2);
}

const text = readFileSync(textFile, "utf-8");
mkdirSync(outDir, { recursive: true });
const tts = new MsEdgeTTS();
await tts.setMetadata(voice, OUTPUT_FORMAT.AUDIO_24KHZ_96KBITRATE_MONO_MP3);
const { audioFilePath } = await tts.toFile(outDir, text);
console.log(audioFilePath);
process.exit(0);
