import { defineUserConfig } from "vuepress";

import theme from "./theme.js";

export default defineUserConfig({
  base: "/RCParsing/",

  lang: "en-US",
  title: "RCParsing",
  description: "RCParsing is the fluent, lightweight and powerful .NET lexerless parsing library for language development (DSL) and data scraping.",

  theme,

  // Enable it with pwa
  // shouldPrefetch: false,
});
