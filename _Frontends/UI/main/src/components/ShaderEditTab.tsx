import { Component, useEffect, useState } from "react";
import { Cs2Checkbox, Cs2FormLine, Cs2Select, DefaultPanelScreen, Entity, GameScrollComponent, Input } from "@klyte45/euis-components";

enum ShaderPropertyType {
  Color = "Color",
  Vector = "Vector",
  Float = "Float",
  Range = "Range",
  Texture = "Texture",
  Int = "Int",
  Keyword = "Keyword",
  RenderQueue = "<RenderQueue>",
  ShaderPass = "ShaderPass"
}

type Properties = {
  Name: string
  Idx: number
  Id: number
  Description: string
  Type: ShaderPropertyType
  Value: string
}

type State = {
  selectedFont?: { name: string },
  fontsLoaded?: { name: string }[],
  loadedProperties?: Properties[]
}

export const ShaderEditTab = (props: {}) => {

  const [currentEntity, setCurrentEntity] = useState(null as Entity | null)
  const [loadedProperties, setLoadedProperties] = useState(null as Properties[] | null)
  const [shaderList, setShaderList] = useState([] as { name: string }[])
  const [shader, setShader] = useState("" as string)
  useEffect(() => {
    engine.call("k45::we.test.getEntity").then(setCurrentEntity);
    engine.call("k45::we.test.listShader").then((x: string[]) => setShaderList(x.filter(y => !y.startsWith("Hidden/")).sort((a, b) => a.localeCompare(b)).map(y => { return { name: y } })))
  }, [])
  useEffect(() => {
    if (currentEntity != null) {
      engine.call("k45::we.test.setEntity", currentEntity?.Index ?? 0, currentEntity?.Version ?? 0).then(x => {
        engine.call("k45::we.test.listCurrentMaterialSettings").then(x => setLoadedProperties(x))
        engine.call("k45::we.test.getShader").then(x => setShader(x))
      });
    }
  }, [currentEntity])
  const saveShader = (x) => {
    setShader(x);
    engine.call("k45::we.test.setShader", x).then(x => engine.call("k45::we.test.listCurrentMaterialSettings").then(x => setLoadedProperties(x)));
  }

  return <>
    <DefaultPanelScreen title="Shader test">
      <div style={{ display: 'flex', flexDirection: "row" }}>
        <Input title="Entity Index" getValue={() => currentEntity?.Index.toString()} onValueChanged={async (y) => { setCurrentEntity({ Index: parseInt(y) || 0, Version: currentEntity?.Version ?? 0 }); return y }} />
        <Input title="Entity Version" getValue={() => currentEntity?.Version.toString()} onValueChanged={async (y) => { setCurrentEntity({ Index: currentEntity?.Index ?? 0, Version: parseInt(y) || 0 }); return y }} />
      </div>
      <GameScrollComponent>
        {loadedProperties && <>
          <Cs2FormLine title="Select Font">
            <Cs2Select
              options={shaderList}
              getOptionLabel={(x) => x.name}
              getOptionValue={(x) => x.name}
              onChange={(x) => saveShader(x.name)}
              value={{ name: shader }} />
          </Cs2FormLine>
          {loadedProperties.sort((a, b) => a.Name.localeCompare(b.Name))
            .map((x) => {
              if (x.Type == ShaderPropertyType.Keyword || x.Type == ShaderPropertyType.ShaderPass) {
                return <Cs2FormLine key={x.Idx} title={x.Name} subtitle={<div style={{ display: "flex" }}><b style={{ color: "lime" }}>{x.Type}</b> - {x.Description}</div>}>
                  <Cs2Checkbox isChecked={() => x.Value === "True"} onValueToggle={async (y) => { x.Value = await engine.call("k45::we.test.setCurrentMaterialSettings", (x.Type == ShaderPropertyType.Keyword ? "k" : "p") + x.Name, y ? "True" : "False"); }} />
                </Cs2FormLine>
              } else if (x.Type == ShaderPropertyType.RenderQueue) {
                return <Input key={x.Idx} title={x.Name} subtitle={<div style={{ display: "flex" }}><b style={{ color: "lime" }}>Int</b> - {x.Description}</div>} getValue={() => x.Value} onValueChanged={async (y) => await engine.call("k45::we.test.setCurrentMaterialSettings", x.Type, y)} />
              } else {
                return <Input key={x.Idx} title={x.Name} subtitle={<div style={{ display: "flex" }}><b style={{ color: "lime" }}>{x.Type}</b> - {x.Description}</div>} getValue={() => x.Value} onValueChanged={async (y) => await engine.call("k45::we.test.setCurrentMaterialSettings", x.Idx + "", y)} />
              }
            })}
        </>
        }
      </GameScrollComponent>
    </DefaultPanelScreen>
  </>;

}
