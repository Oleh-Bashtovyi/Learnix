const path = require('path');

module.exports = {
  // Prettier for frontend files
  "learnix-client/src/**/*.{ts,tsx,js,jsx}": (filenames) => {
    const files = filenames.join(' ');
    return [
      `npm --prefix learnix-client run lint:staged -- --fix ${files}`,
      `npm --prefix learnix-client run format:staged -- ${files}`
    ];
  },
  "learnix-client/src/**/*.{css,scss,md}": (filenames) => {
    const files = filenames.join(' ');
    return [
      `npm --prefix learnix-client run format:staged -- ${files}`
    ];
  },

  // Dotnet Format for backend C# files — formats only staged files
  "Learnix.Backend/**/*.{cs,csproj}": (filenames) => {
    // dotnet format requires the --include flag followed by a space-separated list of relative files.
    // NOTE: dotnet build is intentionally NOT here. lint-staged splits large commits into parallel
    // chunks and runs all returned commands simultaneously per chunk. Running dotnet build in parallel
    // causes a race condition: multiple MSBuild processes try to write to the same .dll at once
    // (CS2012: Cannot open file ... for writing — used by another process).
    // Build validation is done once, sequentially, in .husky/pre-commit after lint-staged finishes.

    // Converts absolute paths to relative to prevent silent failures in dotnet format
    const relativeFiles = filenames.map(f => path.relative(process.cwd(), f)).join(' ');

    return [
      `dotnet format Learnix.Backend/Learnix.Backend.slnx --no-restore --include ${relativeFiles}`
    ];
  }
};
