module.exports = {
  // Prettier for frontend files
  "learnix-client/src/**/*.{ts,tsx,js,jsx}": (filenames) => {
    const files = filenames.join(' ');
    return [
      `cd learnix-client && npx eslint --fix ${files}`,
      `cd learnix-client && npx prettier --write --ignore-unknown ${files}`
    ];
  },
  "learnix-client/src/**/*.{css,scss,md}": (filenames) => {
    const files = filenames.join(' ');
    return [
      `cd learnix-client && npx prettier --write --ignore-unknown ${files}`
    ];
  },
  
  // Dotnet Format for backend C# files — formats only staged files
  "Learnix.Backend/**/*.cs": (filenames) => {
    // dotnet format requires the --include flag followed by a space-separated list of files.
    // NOTE: dotnet build is intentionally NOT here. lint-staged splits large commits into parallel
    // chunks and runs all returned commands simultaneously per chunk. Running dotnet build in parallel
    // causes a race condition: multiple MSBuild processes try to write to the same .dll at once
    // (CS2012: Cannot open file ... for writing — used by another process).
    // Build validation is done once, sequentially, in .husky/pre-commit after lint-staged finishes.
    const files = filenames.join(' ');
    return [
      `dotnet format Learnix.Backend/Learnix.Backend.slnx --no-restore --include ${files}`
    ];
  }
};
