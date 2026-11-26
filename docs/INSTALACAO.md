# Instalação do IT Digitação 2.0.0-rc.1

1. Baixe o pacote portátil e o arquivo SHA256SUMS.txt na [pré-release oficial](https://github.com/NathanCapoeira/IT-digitacao-train/releases/tag/v2.0.0-rc.1).
2. Compare o SHA-256 do ZIP com o arquivo de checksums. No PowerShell, na pasta do download, rode `Get-FileHash -Algorithm SHA256 .\IT-Digitacao-2.0.0-rc.1-Portable-x86.zip`.
3. Clique no ZIP com o botão direito e escolha **Extrair Tudo**. Use uma pasta local acessível à conta do professor.
4. Abra **IT-Digitacao.exe**. Mantenha o catálogo, a DLL e o arquivo de configuração na mesma pasta.
5. Para criar um atalho, clique no EXE com o botão direito e use **Enviar para → Área de trabalho (criar atalho)**. Repita a extração ou copie a pasta extraída para cada computador.

## Runtime

Use o .NET Framework 4.8 ou 4.8.1, conforme o sistema. Se o runtime faltar em um computador offline, baixe o instalador offline oficial em outra máquina, transfira-o por mídia confiável e siga a política de instalação do Instituto. A instalação do runtime pode exigir privilégios administrativos. O aplicativo roda como usuário comum. [Microsoft .NET Framework 4.8](https://dotnet.microsoft.com/pt-br/download/dotnet-framework/net48)

Windows 7 SP1 requer os pré-requisitos indicados pela Microsoft e aguarda validação. É um sistema fora do suporte regular. Quando possível, mantenha as máquinas legadas isoladas da internet. [Requisitos de sistema da Microsoft](https://learn.microsoft.com/pt-br/dotnet/framework/get-started/system-requirements)

## Problemas comuns

- Se o Windows pedir um runtime, instale o runtime do .NET Framework, seguindo a política do Instituto.
- Se a Biblioteca indicar catálogo ausente ou inválido, confira o hash e extraia novamente o ZIP completo.
- Se as preferências não gravarem, verifique a permissão de escrita do perfil. Preferências inválidas carregam os padrões.
- Consulte `BUILD-INFO.json` para conferir versão e commit. Se o aplicativo fechar inesperadamente, registre apenas o cenário e o código do erro do Windows. Evite dados ou texto digitado por alunos.
