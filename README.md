# angular_translation_file_creator

Windows tool to scan on your angular-project for translation-identifier (pattern-based) and write them grouped by files in multiple translation Json-files.

Angular version: >= 2
translation-package: [ngx-translate](https://github.com/ngx-translate)

# Introduction

NGX-Translate is an internationalization library for Angular. It lets you define translations for your content in different languages and switch between them easily. Your translations are saved in json-files and are loaded from your app with for example [ngx-translate/http-loader](https://github.com/ngx-translate/http-loader).
Foreach language you create an own json-file (e.g. en-US.json). The json-file includes for each translation an translation-identifier followed by the translation text.

en-US.json:
    {
      "user": "user",
      "password": "password",
      "login": "login",
      "loginFailed": "Login failed! Please retry."
    }

In your html-Code it looks for example like:
login.component.html:
    <label for="username">{{ 'user' | translate }}</label>
    <input type="text" id="username" name="username">
    <label for="password">{{ 'password' | translate }}</label>
    <input type="text" id="password" name="password">
    <button type="button" (click)="login()">{{ 'login' | translate }}</button>

There are many tutorials for angular internationalization with ngx-translate

At big projects, it is difficult to organize all translations. It isn't clear, wherever the translations in the app are used.
To optimize this, the angular_translation_file_creator organizes the json-files for you, structuring it by the files, where the translations used.

# Usage

That the angular_translation_file_creator can detect your translation identifiers in your code, they must be significantly. Default it marked by the leading string marker ##.

In your html-Code it looks for example like:
login.component.html:
    <label for="username">{{ '##user' | translate }}</label>
    <input type="text" id="username" name="username">
    <label for="password">{{ '##password' | translate }}</label>
    <input type="text" id="password" name="password">
    <button type="button" (click)="login()">{{ '##login' | translate }}</button>
    
login.component.ts:
    [..]
    loginFailed(): void {
      console.error(translateService.translate('##loginFailed'));
    }
    [..]

After starting the "NgxTranslationCreator.exe" you have some configurations:
__Search-Directory__: Select the directory, where the scan should start. The scan includes automatically all subfolders.
__Target-Directory__: Select the output-directory for the translation json-files. The directory should exists.
__Update File - not overwrite__: if checked, the app would not delete translations from the existing translation json-file. Please uncheck this only, if no file exists.
__Delete translations, that not inside code founded__: if checked, the app removes all translations form existings translation json-file, their identifier couln't find in your project code. All removed Translations would be exported in new a file "[filename]_remaining_[timestamp].log.json"

You have some more configuration at the "NgxTranslationCreator.exe.config". Open it with an Editor (e.g. notepad++, VS Code).
NgxTranslationCreator.exe.config:
    <configuration>
      [..]
      <appSettings>
        <!-- Which filetypes the app should scan - Sperarate multiple types with |  -->
        <!-- Example: <add key="sourceFileSearchingPatterns" value="*.html|*.ts" /> -->
        <add key="sourceFileSearchingPatterns" value="*.html|*.ts" />
        <!-- Regex-marker to find translate-identifiers in your files-->
        <!-- Example: <add key="translateSearchingPattern" value="'##[a-zA-Z0-9-_]+'" /> -->
        <add key="translateSearchingPattern" value="'##[a-zA-Z0-9-_]+'" />
        <!-- Which language-files the app should create - Sperarate multiple with |  -->
        <!-- Example: <add key="languageFileNames" value="en-US|de-DE" /> -->
        <add key="languageFileNames" value="en-US|de-DE" />
        <!-- identifier for components in translation-file -->
        <!-- Example: <add key="jsonGroupIdentifier" value="##" /> -->
        <add key="jsonGroupIdentifier" value="##" />
        <!-- start-section-identifier for components in translation file -->
        <!-- Example: <add key="jsonGroupStartIdentifier" value="##start" /> -->
        <add key="jsonGroupStartIdentifier" value="##start" />
        <!-- end-section-identifier for components in translation file -->
        <!-- Example: <add key="jsonGroupEndIdentifier" value="##end" /> -->
        <add key="jsonGroupEndIdentifier" value="##end" />
        [..]
      </appSettings>
      [..]
    </configuration>

