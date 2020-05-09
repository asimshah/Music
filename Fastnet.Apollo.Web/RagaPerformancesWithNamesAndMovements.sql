/****** Script for SelectTopNRows command from SSMS  ******/
SELECT
	rp.[PerformanceId] as 'pid'
      ,rp.[ArtistId] as 'aid'
	  ,a.[Name] as 'artist'
	  ,r.Id as [rid]
	  ,r.[Name] as 'raga'
	  ,t.MovementNumber as 'mn'
	  ,t.Title
      , pf.Id as 'pfid'
      ,pf.[Name]
  FROM [music].[RagaPerformances] rp
  inner join music.Artists a on rp.ArtistId = a.Id
  inner join music.Ragas r on rp.RagaId = r.Id
  inner join music.Tracks t on rp.PerformanceId = t.PerformanceId
  inner join music.PerformancePerformers pp on pp.PerformanceId = rp.PerformanceId
  inner join music.Performers pf on pf.Id = pp.PerformerId
  order by rp.PerformanceId, t.MovementNumber