using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE
{
    public unsafe partial struct WETextDataStruct : ISerializable
    {
        public const int CURRENT_VERSION = 1;
        public static readonly int SIZE = sizeof(WETextDataStruct);

        public bool IsInitialized { get; private set; }
        public float3 offsetPosition = default;
        public float3 offsetRotation = default;
        public float3 scale = Vector3.one;
        public FixedString32Bytes itemName = default;
        public FixedString512Bytes text = default;
        public FixedString32Bytes atlas = default;
        public WEShader shader = default;
        public WESimulationTextType textType = default;
        public WETextDataDefaultStyleStruct defaultStyle = default;
        public WETextDataGlassStyleStruct glassStyle = default;
        public FixedString512Bytes formulae = default;
        public FixedString32Bytes fontName = default;
        public float maxWidthMeters = default;
        public int decalFlags = WETextData.DEFAULT_DECAL_FLAGS;

        public WETextDataStruct()
        {
            IsInitialized = true;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(offsetPosition);
            writer.Write(offsetRotation);
            writer.Write(scale);
            writer.Write(itemName);
            writer.Write(text);
            writer.Write(atlas);
            writer.Write((int)shader);
            writer.Write((int)textType);
            writer.Write(formulae);
            writer.Write(fontName);
            writer.Write(maxWidthMeters);
            writer.Write(decalFlags);
            switch (shader)
            {
                case WEShader.Default: writer.Write(defaultStyle); break;
                case WEShader.Glass: writer.Write(glassStyle); break;
            }
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int version);
            if (version > CURRENT_VERSION)
            {
                LogUtils.DoWarnLog($"Invalid version for {GetType()}: {version}");
                return;
            }
            reader.Read(out offsetPosition);
            reader.Read(out offsetRotation);
            reader.Read(out scale);
            reader.Read(out itemName);
            reader.Read(out text);
            reader.Read(out atlas);
            reader.Read(out int shader); this.shader = (WEShader)shader;
            reader.Read(out int textType); this.textType = (WESimulationTextType)textType;
            if (version == 0)
            {
                reader.Read(out WETextDataStyleStruct oldStyle);
                defaultStyle = oldStyle.ToDefaultStyle();
                glassStyle = oldStyle.ToGlassStyle();
            }
            reader.Read(out formulae);
            reader.Read(out fontName);
            reader.Read(out maxWidthMeters);
            reader.Read(out decalFlags);
            if (version >= 1)
            {
                switch (this.shader)
                {
                    case WEShader.Default: reader.Read(out defaultStyle); break;
                    case WEShader.Glass: reader.Read(out glassStyle); break;
                }
            }
            IsInitialized = true;
        }

        internal static WETextDataStruct FromXml(WETextDataXml data)
            => data is null
                ? default
                : new WETextDataStruct
                {
                    IsInitialized = true,
                    offsetPosition = data.offsetPosition,
                    offsetRotation = data.offsetRotation,
                    scale = data.scale,
                    itemName = data.itemName ?? "",
                    text = data.text?.defaultValue ?? "",
                    atlas = data.atlas ?? "",
                    shader = data.shader,
                    textType = data.textType,
                    glassStyle = WETextDataGlassStyleStruct.FromXml(data.glassStyle),
                    defaultStyle = WETextDataDefaultStyleStruct.FromXml(data.defaultStyle),
                    formulae = data.text?.formulae ?? "",
                    fontName = data.fontName ?? "",
                    maxWidthMeters = data.maxWidthMeters,
                    decalFlags = data.decalFlags,

                };

        internal WETextDataXml ToXml() => new()
        {
            offsetPosition = (Vector3Xml)offsetPosition,
            offsetRotation = (Vector3Xml)offsetRotation,
            scale = (Vector3Xml)scale,
            itemName = itemName.ToString(),
            text = new()
            {
                defaultValue = text.ToString(),
                formulae = formulae.ToString(),
            },
            atlas = atlas.ToString(),
            shader = shader,
            textType = textType,
            defaultStyle = defaultStyle.ToXml(),
            glassStyle = glassStyle.ToXml(),
            fontName = fontName.ToString(),
            maxWidthMeters = maxWidthMeters,
            decalFlags = decalFlags,

        };
    }
}