# Unfuscator

A Dotfuscator de-obfuscator. 

# Usage

```csharp
         // Initialize an empty mapping
         IMapping mapping = Mapping.Empty();         
          
         // Load all files from the directory that have the format Map-1.2.3.xml
         mapping.LoadDotfuscator(MapFileDirectory, 
            (path, ex) => Console.WriteLine($"error loading {path} : {ex}"), 
            (path, progress) => Console.WriteLine($"Loading...{path}: {progress*100:F0}%"));

         // Create an unfuscator objcect that uses the mapping.         
         var unfuscator = new UnObfuscator(mapping);
          
         var unobfuscated = unfuscator.Unfuscate(myStackTrace);
         
         var resultWriter = ResultWriter.PlainText; // Or ResultWriter.Json, Xml
         
         Console.WriteLine("\nUnobfuscated:\n");
         resultWriter.Write(unobfuscated, null, Console.Out);         

         Console.ReadKey();
```
# Limitations

Ignores a lot of anonymous types, generic methods etc.
         
# Example UI

unfuscator.ui contains a simple WinForms test app. 
         
 
