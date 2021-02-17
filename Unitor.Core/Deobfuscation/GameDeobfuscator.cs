using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unitor.Core.Reflection;

namespace Unitor.Core.Deobfuscation
{
    public static class GameDeobfuscator
    {
        public static void Deobfuscate(Game game, string path, EventHandler endCallback)
        {
            if (!File.Exists(path))
            {
                //file doesnt exist err
            }
            if(Directory.Exists(path))
            {
                //is folder err
            }

            if(Path.GetExtension(path) == ".dll")
            {
                throw new NotImplementedException("Il2CppTranslator class loading not yet implemented");
            }
            else if (Path.GetExtension(path) == ".json")
            {
                string json = File.ReadAllText(path);
                
                JsonTranslations translations = JsonLoader.DeserialzeTranslations(json);
                if(translations != null)
                {
                    foreach(var translation in translations.Types)
                    {
                        UnitorType type = game.Model.Types.FirstOrDefault(t => t.CSharpName == translation.Key); 
                        if(type != null)
                        {
                            type.Name = translation.Value;
                        }
                        
                    }
                    List<UnitorField> fields = game.Model.Types.SelectMany(t => t.Fields).ToList();
                    foreach (var translation in translations.Fields)
                    {
                        UnitorField field = fields.FirstOrDefault(t => t.CSharpName == translation.Key);
                        if (field != null)
                        {
                            field.Name = translation.Value;
                        }
                    }
                    endCallback.Invoke(null, null);
                    return;
                }

                List<JsonTypeMapping> mappings = JsonLoader.DeserialzeMappings(json);
                if(mappings != null)
                {
                    endCallback.Invoke(null, null);
                    throw new NotImplementedException("Deobfuscation using mappings is not yet implemented");
                }
                endCallback.Invoke(null, null);
                throw new ArgumentException("Invalid json formatting neither translations nor mappings");
            }
        }
    }
}
