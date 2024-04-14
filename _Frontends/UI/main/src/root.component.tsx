///<reference path="euis.d.ts" />

import { BaseTab } from "#components/BaseTab";
import { ShaderEditTab } from "#components/ShaderEditTab";
import { MainSideTabMenuComponent, MenuItem } from "@klyte45/euis-components";
import { Component } from "react";
import { Tab, TabList, TabPanel, Tabs } from "react-tabs";


export type Entity = {
  __Type: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null';
  Index: number;
  Version: number;
};
const menus: MenuItem[] = [
  {
    iconUrl: "coui://GameUI/Media/Glyphs/Gear.svg",
    name: "Main",
    panelContent: <BaseTab />,
    tintedIcon: true
  },
  {
    iconUrl: "coui://we.k45/UI/images/WE.svg",
    name: "Shader Editing",
    panelContent: <ShaderEditTab />
  }
]

export default class Root extends Component<{}> {

  constructor(props) {
    super(props)
  }

  componentDidMount(): void {
    engine.on("k45::we.test.enableTestTool->", this.onSelectEntity)
    engine.on("k45::we.test.fontsChanged->", this.onFontsChanged)
    engine.call("k45::we.test.listFonts").then((x) => this.setState({ fontsLoaded: (x as string[]).map(x => { return { name: x } }) }));
  }
  componentWillUnmount(): void {
    engine.off("k45::we.test.enableTestTool->")
  }

  private onSelectEntity = (e: Entity) => {
    console.log(e)
    this.setState({ currentEntity: e });
  }
  private onFontsChanged = (e: string[]) => {
    console.log(e)
    this.setState({ fontsLoaded: (e as string[]).map(x => { return { name: x } }) });
  }

  render() {
    return <ErrorBoundary>
      <MainSideTabMenuComponent
        items={menus}
        mainIconUrl="coui://we.k45/UI/images/WE.svg"
        modTitle="Write Everywhere"
        subtitle="Main settings window"
      />
    </ErrorBoundary>;
  }
}

class ErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false };
  }

  componentDidCatch(error, info) {
    // Display fallback UI
    this.setState({ hasError: true });
    // You can also log the error to an error reporting service
    console.log(error, info);
  }

  render() {
    if ((this.state as any)?.hasError) {
      // You can render any custom fallback UI
      return <h1>Something went wrong.</h1>;
    }
    return this.props.children;
  }
}




