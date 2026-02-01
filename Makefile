.PHONY: build test test-pipeline test-hardening fuzz run demo clean pack publish help

# Default target
help:
	@echo "Axom - Available commands:"
	@echo "  make build      - Build the solution"
	@echo "  make test       - Run tests"
	@echo "  make test-pipeline - Run CompilerPipelineTests"
	@echo "  make test-hardening - Run golden/snapshot tests"
	@echo "  make fuzz       - Run fuzz harness (short)"
	@echo "  make run FILE=hello.axom - Compile and run a Axom file"
	@echo "  make compile FILE=hello.axom - Compile a Axom file (build only)"
	@echo "  make demo      - Run a quick demo (print \"ciao\")"
	@echo "  make pack      - Build the NuGet package (tool)"
	@echo "  make publish PACKAGE=... - Push NuGet package"
	@echo "  make clean      - Clean build artifacts"
	@echo "  make help       - Show this help message"

# Build the solution
build:
	dotnet build

# Run tests
test:
	dotnet test

# Run the end-to-end pipeline test
test-pipeline:
	dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "FullyQualifiedName~CompilerPipelineTests.Compile_print_string_generates_console_write"

# Run golden/snapshot tests
test-hardening:
	dotnet test tests/Axom.Tests/Axom.Tests.csproj --filter "FullyQualifiedName~CodegenGoldenTests|FullyQualifiedName~CodegenGoldenCoverageTests|FullyQualifiedName~DiagnosticsSnapshotTests"

# Run fuzz harness (short)
fuzz:
	dotnet run --project tests/Axom.Fuzz -- --iterations 100 --max-length 128

# Compile a Axom file (build only)
compile:
	@if [ -z "$(FILE)" ]; then \
		echo "Usage: make compile FILE=hello.axom"; \
		exit 1; \
	fi
	dotnet run --project src/axom -- build $(FILE)

# Compile and run a Axom file
run:
	@if [ -z "$(FILE)" ]; then \
		echo "Usage: make run FILE=hello.axom"; \
		exit 1; \
	fi
	dotnet run --project src/axom -- run $(FILE)

# Quick demo run
demo:
	@printf 'print "ciao"\n' > out/demo.axom
	dotnet run --project src/axom -- run out/demo.axom

# Build the NuGet package
pack:
	dotnet pack src/axom -c Release

# Push the NuGet package
publish:
	@if [ ! -f api.key ]; then \
		echo "Missing api.key file (NuGet API key)."; \
		exit 1; \
	fi
	@if ! ls $(PACKAGE) >/dev/null 2>&1; then \
		echo "Package not found. Use PACKAGE=path/to/*.nupkg"; \
		exit 1; \
	fi
	dotnet nuget push $(PACKAGE) -k "$$(< api.key)" -s https://api.nuget.org/v3/index.json --skip-duplicate

# Clean build artifacts
clean:
	dotnet clean
	rm -rf out
	rm -rf */bin */obj
	rm -rf tests/*/bin tests/*/obj
	rm -rf src/*/bin src/*/obj
