<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet 
  version="1.0" 
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:c="http://tempuri.org/schema/catalog#" 
  xmlns:m="http://tempuri.org/schema/music#" 
  xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
  xmlns:mc="http://tempuri.org/schema/music_catalog"
  exclude-result-prefixes="mc"
>
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/mc:band">
    <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
      <m:Band>

        <xsl:variable name="category" select="mc:category" />
        <xsl:variable name="id" select="mc:id" />

        <xsl:attribute name="rdf:about">
          <xsl:value-of select="concat('http://tempuri.org/british/music#', $category, '/', $id)"/>
        </xsl:attribute>

        <c:name>
          <xsl:value-of select="mc:name"/>
        </c:name>

        <xsl:for-each select="mc:member">
          <xsl:variable name="member" select="." />
          <c:member>
            <rdf:Description>
              <xsl:attribute name="rdf:about">
                <xsl:value-of select="concat('http://tempuri.org/schema/music#', $category, '/', $member)"/>
              </xsl:attribute>
            </rdf:Description>
          </c:member>
        </xsl:for-each>

      </m:Band>
    </rdf:RDF>
  </xsl:template>
  
</xsl:stylesheet>
