import { Component } from "react";
import { Cs2Checkbox, Cs2FormLine, Cs2Select, DefaultPanelScreen, GameScrollComponent, Input } from "@klyte45/euis-components";

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

export class ShaderEditTab extends Component<{}, State> {

  constructor(props) {
    super(props);
  }

  componentDidMount(): void {
    engine.on("k45::we.test.fontsChanged->", this.onFontsChanged);
    engine.call("k45::we.test.listFonts").then((x) => this.setState({ fontsLoaded: (x as string[]).map(x => { return { name: x }; }) }));
  }
  componentWillUnmount(): void {
    engine.off("k45::we.test.enableTestTool->");
  }

  private onFontsChanged = (e: string[]) => {
    console.log(e);
    this.setState({ fontsLoaded: (e as string[]).map(x => { return { name: x }; }) });
  };

  render() {
    return <>
      <DefaultPanelScreen title="Shader test">
        <Cs2FormLine title="Select Font">
          <Cs2Select
            options={this.state?.fontsLoaded}
            getOptionLabel={(x) => x.name}
            getOptionValue={(x) => x.name}
            onChange={(x) => this.onFontSet(x)}
            value={this.state?.selectedFont} />
        </Cs2FormLine>
        <GameScrollComponent>
          {
            this.state?.loadedProperties && <>
              {this.state.loadedProperties.sort((a, b) => a.Name.localeCompare(b.Name))
                .map((x) => {
                  if (x.Type == ShaderPropertyType.Keyword || x.Type == ShaderPropertyType.ShaderPass) {
                    return <Cs2FormLine key={x.Idx} title={x.Name} subtitle={<div style={{ display: "flex" }}><b style={{ color: "lime" }}>{x.Type}</b> - {x.Description}</div>}>
                      <Cs2Checkbox isChecked={() => x.Value === "True"} onValueToggle={async (y) => { x.Value = await engine.call("k45::we.test.setCurrentMaterialSettings", this.state.selectedFont.name, (x.Type == ShaderPropertyType.Keyword ? "k" : "p") + x.Name, y ? "True" : "False"); this.setState({}) }} />
                    </Cs2FormLine>
                  } else if (x.Type == ShaderPropertyType.RenderQueue) {
                    return <Input key={x.Idx} title={x.Name} subtitle={<div style={{ display: "flex" }}><b style={{ color: "lime" }}>Int</b> - {x.Description}</div>} getValue={() => x.Value} onValueChanged={async (y) => await engine.call("k45::we.test.setCurrentMaterialSettings", this.state.selectedFont.name, x.Type, y)}>
                    </Input>
                  } else {
                    return <Input key={x.Idx} title={x.Name} subtitle={<div style={{ display: "flex" }}><b style={{ color: "lime" }}>{x.Type}</b> - {x.Description}</div>} getValue={() => x.Value} onValueChanged={async (y) => await engine.call("k45::we.test.setCurrentMaterialSettings", this.state.selectedFont.name, x.Idx + "", y)}>
                    </Input>
                  }
                })}
            </>
          }
        </GameScrollComponent>
      </DefaultPanelScreen>
    </>;
  }
  onFontSet(x: { name: string; }): void {
    this.setState({ selectedFont: x }, () => this.updateFonts());
  }
  async updateFonts() {
    var properties = await engine.call("k45::we.test.listCurrentMaterialSettings", this.state.selectedFont.name);
    console.log(properties);
    this.setState({ loadedProperties: properties })
  }
}
