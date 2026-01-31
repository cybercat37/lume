_lume_complete()
{
    local cur prev
    cur="${COMP_WORDS[COMP_CWORD]}"
    prev="${COMP_WORDS[COMP_CWORD-1]}"

    if [[ ${COMP_CWORD} -eq 1 ]]; then
        COMPREPLY=( $(compgen -W "check build run" -- "${cur}") )
        return 0
    fi

    if [[ "${prev}" == "--out" ]]; then
        COMPREPLY=( $(compgen -d -- "${cur}") )
        return 0
    fi

    COMPREPLY=( $(compgen -W "--out --quiet --verbose --cache --help --version" -- "${cur}") )
    return 0
}

complete -F _lume_complete lume
