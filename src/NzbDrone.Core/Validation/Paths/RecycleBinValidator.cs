using System;
using System.IO;
using FluentValidation.Validators;

namespace NzbDrone.Core.Validation.Paths
{
    public class RecycleBinValidator : PropertyValidator
    {
        protected override string GetDefaultMessageTemplate() => "Path '{path}' is {relationship} recycle bin folder";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var folder = context.PropertyValue.ToString();
            context.MessageFormatter.AppendArgument("path", folder);

            var directory = new DirectoryInfo(folder);

            if (directory.Name.Equals(".bin", StringComparison.InvariantCultureIgnoreCase))
            {
                context.MessageFormatter.AppendArgument("relationship", "set to");

                return false;
            }

            directory = directory.Parent;

            while (directory != null)
            {
                if (directory.Name.Equals(".bin", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.MessageFormatter.AppendArgument("relationship", "child of");

                    return false;
                }

                directory = directory.Parent;
            }

            return true;
        }
    }
}
