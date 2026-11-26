# IT Digitação

**Instituto de Transformação • Fases 2 e 3 • Versão 1.1.0**

*Aprenda. Pratique. Transforme.*

O IT Digitação é um aplicativo educacional para aproveitar o tempo restante da aula com atividades de digitação. A proposta é desenvolver precisão, concentração, leitura e vocabulário, com conteúdo voltado principalmente a crianças e adolescentes.

## Tela inicial

Escolha quanto tempo você tem para praticar, de 5 a 30 minutos. **Começar minha aventura** abre a Biblioteca e mostra a duração escolhida. Voltar ao início preserva a escolha. Biblioteca, Como digitar e Configurações já podem ser abertas e indicam o conteúdo previsto para as próximas fases.

A janela é redimensionável. Os cartões alternam entre seis colunas e duas fileiras de três conforme a largura disponível. A aplicação usa recursos locais e funciona offline.

### Baixar o aplicativo

Na página [GitHub Actions](https://github.com/NathanCapoeira/IT-digitacao-train/actions), abra a execução bem-sucedida mais recente de **Build do IT Digitação**. Com sua conta do GitHub conectada, baixe **IT-Digitacao-x86-…** na seção **Artifacts**. Extraia todos os arquivos e abra `IT-Digitacao.exe` na pasta extraída.

## Estrutura

| Caminho | Conteúdo |
| --- | --- |
| `ITDigitacao.sln` | Solução MSBuild com configuração Release x86 |
| `src/ITDigitacao.App/` | Aplicativo WPF em C# e XAML para .NET Framework 4.8 |
| `src/ITDigitacao.App/Themes/` | Paleta, ícones vetoriais e componentes compartilhados |
| `src/ITDigitacao.App/Views/HomeView.xaml` | Banner, escolha de duração e benefícios |
| `src/ITDigitacao.App/Assets/` | Mascote e marcas com Build Action Resource |
| `tests/smoke-wpf.ps1` | Teste de recursos, layout e navegação em Windows |
| `scripts/build.ps1` | Descoberta do MSBuild, compilação limpa e preparação da distribuição |
| `.github/workflows/build-windows.yml` | Compilação no Windows e publicação do artefato de CI |

## Pré-requisitos no Windows

1. Instale Git e [Visual Studio Code](https://code.visualstudio.com/). Se desejar usar o comando `code`, habilite a opção de adicionar o VS Code ao PATH no instalador.
2. Na página de downloads da Microsoft, instale o **Build Tools for Visual Studio 2022**. No Visual Studio Installer, selecione a carga de trabalho **.NET desktop build tools**. Em **Componentes individuais**, confirme o MSBuild, o compilador C# e o **.NET Framework 4.8 targeting pack**. Basta instalar o Build Tools.
3. Se o pacote de referência v4.8 estiver ausente, instale o [Developer Pack do .NET Framework 4.8](https://dotnet.microsoft.com/pt-br/download/dotnet-framework/net48), que fornece as referências para compilação. A máquina que apenas executa o aplicativo precisa do runtime .NET Framework 4.8 ou de uma versão 4.x posterior que o substitua.
4. Use o **Windows PowerShell 5.1** no terminal integrado do VS Code. A extensão C# é opcional para edição. O script descobre o MSBuild por meio do `vswhere`, com o PATH como alternativa.

[Downloads do Build Tools](https://visualstudio.microsoft.com/pt-br/downloads/)

## Compilar e executar

Para obter o código e compilar, rode todo o bloco no PowerShell:

```powershell
git clone https://github.com/NathanCapoeira/IT-digitacao-train.git
Set-Location IT-digitacao-train
code .
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
if ($LASTEXITCODE -ne 0) { throw "O build falhou. Consulte a saída acima." }
Get-Item .\dist\IT-Digitacao.exe
& .\dist\IT-Digitacao.exe
```

Para um checkout existente atualizado, comece na raiz do projeto e execute a partir de `code .`. A opção `-ExecutionPolicy Bypass` vale somente para o processo PowerShell iniciado pelo comando.

O script chama o MSBuild com `Rebuild`, `Configuration=Release` e `Platform=x86`. A pasta `dist` é preparada a partir de uma compilação bem-sucedida e contém `IT-Digitacao.exe` e `IT-Digitacao.exe.config`. Mantenha os dois arquivos juntos ao distribuir.

### Diagnóstico

- **MSBuild ausente** — revise a carga de trabalho .NET desktop build tools no Visual Studio Installer.
- **Referências v4.8 ausentes ou MSB3644** — instale o Developer Pack ou o targeting pack **4.8**.
- **Falha do MSBuild** — examine o primeiro erro informado no terminal. Para salvar a saída mantendo o código de saída do processo:

  ```powershell
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 2>&1 |
      Tee-Object -FilePath "$env:TEMP\IT-Digitacao-build.log"
  $buildExitCode = $LASTEXITCODE
  Get-Item "$env:TEMP\IT-Digitacao-build.log"
  if ($buildExitCode -ne 0) { throw "Falha de compilação ($buildExitCode)." }
  ```

  Após uma falha, a pasta `dist` pode ainda conter uma distribuição anterior. Só use o executável como resultado da compilação atual se o comando terminar com código zero.

## GitHub Actions

O workflow **Build do IT Digitação** roda em Windows nas atualizações de `main`, nos pull requests para `main` e por disparo manual em **Actions → Build do IT Digitação → Run workflow**. Após um job bem-sucedido, baixe **IT-Digitacao-x86-…** na seção **Artifacts** da execução. O arquivo `build-info.txt` registra o commit compilado. A execução também publica a captura gerada pelo teste WPF. A distribuição contém a versão de desenvolvimento.

## Validação no Windows

Após um build bem-sucedido, abra o executável e confirme:

1. Confira as marcas no cabeçalho e rodapé e o mascote no banner.
2. Escolha uma duração diferente de 10 minutos e selecione **Começar minha aventura**. Confira a duração na Biblioteca e volte ao início.
3. Abra **Como digitar** e **Configurações**. Use Tab e as setas nas opções de duração para conferir foco e seleção.
4. Reduza e maximize a janela, confira o rearranjo dos seis cartões e use **Sair**.

A validação de Windows 7 SP1 depende dos pré-requisitos do framework e de teste real. Compatibilidade nas versões alvo, uso em máquinas de 2 GB de RAM e comportamento nas resoluções e escalas de DPI previstas são metas de validação.
