.PHONY: build test test-pipeline run demo clean help

# Default target
help:
	@echo "Lume - Available commands:"
	@echo "  make build      - Build the solution"
	@echo "  make test       - Run tests"
	@echo "  make test-pipeline - Run CompilerPipelineTests"
	@echo "  make run FILE=hello.lume - Compile and run a Lume file"
	@echo "  make compile FILE=hello.lume - Compile a Lume file (build only)"
	@echo "  make demo      - Run a quick demo (print \"ciao\")"
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
	dotnet test tests/Lume.Tests/Lume.Tests.csproj --filter "FullyQualifiedName~CompilerPipelineTests.Compile_print_string_generates_console_write"

# Compile a Lume file (build only)
compile:
	@if [ -z "$(FILE)" ]; then \
		echo "Usage: make compile FILE=hello.lume"; \
		exit 1; \
	fi
	dotnet run --project src/lume -- build $(FILE)

# Compile and run a Lume file
run:
	@if [ -z "$(FILE)" ]; then \
		echo "Usage: make run FILE=hello.lume"; \
		exit 1; \
	fi
	dotnet run --project src/lume -- run $(FILE)

# Quick demo run
demo:
	@printf 'print "ciao"\n' > out/demo.lume
	dotnet run --project src/lume -- run out/demo.lume

# Clean build artifacts
clean:
	dotnet clean
	rm -rf out
	rm -rf */bin */obj
	rm -rf tests/*/bin tests/*/obj
	rm -rf src/*/bin src/*/obj
