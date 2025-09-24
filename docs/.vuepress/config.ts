import { defineUserConfig } from "vuepress";

import theme from "./theme.js";

export default defineUserConfig({
  base: "/RCParsing/",

  lang: "en-US",
  title: "RCParsing",
  description: "RCParsing is the ultimate .NET parsing framework for language development and data scraping",

  theme,

  // Enable it with pwa
  // shouldPrefetch: false,
});
