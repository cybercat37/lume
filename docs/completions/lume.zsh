#compdef lume

_lume() {
  local -a commands
  commands=(check build run)

  _arguments \
    '1:command:->cmds' \
    '*::options:->opts'

  case $state in
    cmds)
      _describe 'command' commands
      ;;
    opts)
      _arguments \
        '--out[override output directory]:directory:_files -/' \
        '--quiet[suppress non-error output]' \
        '--verbose[include extra context]' \
        '--cache[enable compilation cache]' \
        '--help[show usage]' \
        '--version[show version]'
      ;;
  esac
}

_lume
