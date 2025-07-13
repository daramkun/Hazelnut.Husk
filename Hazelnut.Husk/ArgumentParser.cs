using System.Reflection;

namespace Hazelnut.Husk;

public static class ArgumentParser
{
    public static T ParseArguments<T>(this string[] args) =>
        (T)ParseArguments(typeof(T), args, out _);
    
    public static T ParseArguments<T>(this string[] args, out string[] rest) =>
        (T)ParseArguments(typeof(T), args, out rest);

    public static object ParseArguments(Type type, string[] args) =>
        ParseArguments(type, args, out _);

    public static object ParseArguments(Type type, string[] args, out string[] rest)
    {
        if (type.GetCustomAttribute<ArgumentSerializableAttribute>() == null)
            throw new ArgumentException("Type must be marked with ArgumentSerializableAttribute");

        var result = Activator.CreateInstance(type);
        if (result == null)
            throw new ArgumentException("Type must have a constructor with no parameters or a constructor with a single string[] parameter");

        var attrMembers = type.GetMembers()
            .Select(member => (memberInfo: member, attr: member.GetCustomAttribute<ArgumentAttribute>()))
            .Where(pair => pair.attr != null)
            .ToArray();
        
        Queue<string> argsQueue = new(args);
        List<ArgumentAttribute> proceeds = [];
        List<string> restList = [];
        string? currentOption = null;
        var currentOrder = 0;
        while (argsQueue.TryDequeue(out var arg))
        {
            if (arg.StartsWith("--") || arg.StartsWith('-'))
            {
                var equalIndex = arg.IndexOf('=');
                if (equalIndex == -1)
                {
                    var (memberInfo, attr) =
                        attrMembers.FirstOrDefault(pair => pair.attr!.IsNameEquals(arg));
                    if (memberInfo == null || attr == null)
                    {
                        restList.Add(arg);
                        continue;
                    }

                    proceeds.Add(attr);

                    if (memberInfo is PropertyInfo propInfo && propInfo.PropertyType == typeof(bool))
                        propInfo.SetValue(result, true);
                    else if (memberInfo is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(bool))
                    {
                        if (type.IsValueType)
                            fieldInfo.SetValueDirect(__makeref(result), true);
                        else
                            fieldInfo.SetValue(result, true);
                    }
                    else
                        currentOption = arg;
                }
                else
                {
                    var (argName, argValue) = (arg[..equalIndex], arg[(equalIndex + 1)..]);
                    var (memberInfo, attr) =
                        attrMembers.FirstOrDefault(pair => pair.attr!.IsNameEquals(argName));
                    if (memberInfo == null || attr == null)
                    {
                        restList.Add(arg);
                        continue;
                    }

                    proceeds.Add(attr);
                    
                    if (memberInfo is PropertyInfo propInfo)
                    {
                        var value = propInfo.PropertyType == typeof(bool)
                            ? ParserRuntime.ParseBoolean(argValue)
                            : Convert.ChangeType(argValue, propInfo.PropertyType);
                        propInfo.SetValue(result, value);
                    }
                    else if (memberInfo is FieldInfo fieldInfo)
                    {
                        var value = fieldInfo.FieldType == typeof(bool)
                            ? ParserRuntime.ParseBoolean(argValue)
                            : Convert.ChangeType(argValue, fieldInfo.FieldType);
                        if (type.IsValueType)
                            fieldInfo.SetValueDirect(__makeref(result), value);
                        else
                            fieldInfo.SetValue(result, value);
                    }
                }
            }
            else
            {
                if (currentOption != null)
                {
                    var (memberInfo, attr) =
                        attrMembers.FirstOrDefault(pair => pair.attr!.IsNameEquals(currentOption));
                    if (memberInfo == null || attr == null)
                    {
                        restList.Add(currentOption);
                        restList.Add(arg);
                        currentOption = null;
                        continue;
                    }

                    proceeds.Add(attr);
                    
                    if (memberInfo is PropertyInfo propInfo)
                    {
                        var value = propInfo.PropertyType == typeof(bool)
                            ? ParserRuntime.ParseBoolean(arg)
                            : Convert.ChangeType(arg, propInfo.PropertyType);
                        propInfo.SetValue(result, value);
                    }
                    else if (memberInfo is FieldInfo fieldInfo)
                    {
                        var value = fieldInfo.FieldType == typeof(bool)
                            ? ParserRuntime.ParseBoolean(arg)
                            : Convert.ChangeType(arg, fieldInfo.FieldType);
                        if (type.IsValueType)
                            fieldInfo.SetValueDirect(__makeref(result), value);
                        else
                            fieldInfo.SetValue(result, value);
                    }

                    currentOption = null;
                }
                else
                {
                    var (memberInfo, attr) = attrMembers.FirstOrDefault(pair => pair.attr!.Order == currentOrder);
                    ++currentOrder;
                    
                    if (memberInfo == null || attr == null)
                    {
                        restList.Add(arg);
                        currentOption = null;
                        continue;
                    }
                    
                    if (memberInfo is PropertyInfo propInfo)
                    {
                        var value = propInfo.PropertyType == typeof(bool)
                            ? ParserRuntime.ParseBoolean(arg)
                            : Convert.ChangeType(arg, propInfo.PropertyType);
                        propInfo.SetValue(result, value);
                    }
                    else if (memberInfo is FieldInfo fieldInfo)
                    {
                        var value = fieldInfo.FieldType == typeof(bool)
                            ? ParserRuntime.ParseBoolean(arg)
                            : Convert.ChangeType(arg, fieldInfo.FieldType);
                        if (type.IsValueType)
                            fieldInfo.SetValueDirect(__makeref(result), value);
                        else
                            fieldInfo.SetValue(result, value);
                    }
                }
            }
        }

        var unproceedRequiredAttrs = attrMembers
            .Select(pair => pair.attr!)
            .Except(proceeds)
            .Any(attr => attr.IsRequired);
        if (unproceedRequiredAttrs)
            throw new ArgumentException("Required arguments are missing");

        rest = restList.ToArray();
        
        return result;
    }
}