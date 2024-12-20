const { merge } = require("webpack-merge");
const singleSpaDefaults = require("webpack-config-single-spa-react-ts");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const TsconfigPathsPlugin = require("tsconfig-paths-webpack-plugin")

const packageJsonAppName = require('./package.json').name;
const modderId = /^@([a-z0-9\-]+)\//g.exec(packageJsonAppName)[1]
const appId = /\/([a-z0-9\-]+)$/g.exec(packageJsonAppName)[1]
console.log(`modderId = ${modderId}; appId = ${appId}`);

module.exports = (webpackConfigEnv, argv) => {
  const defaultConfig = singleSpaDefaults({
    orgName: modderId,
    projectName: appId,
    webpackConfigEnv,
    argv,
  });

  return merge(defaultConfig, {
    entry: "./src/react-app.tsx",
    resolve: {
      plugins: [
        new TsconfigPathsPlugin()
      ]
    },
    plugins: [
      new MiniCssExtractPlugin({
        filename: `${modderId}-${appId}.css`
      })
    ],
    module: {
      rules: [
        {
          test: /\.(s[ac])ss$/i,
          use: [
            MiniCssExtractPlugin.loader,
            "css-loader",
            "sass-loader",
          ],
        },
        {
          test: /\.css$/i,
          use: [
            MiniCssExtractPlugin.loader,
            "css-loader",
          ],
        }
      ],
    },
  });
};
